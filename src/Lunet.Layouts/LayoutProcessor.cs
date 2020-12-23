﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Scripts;
using Scriban;
using Scriban.Syntax;
using Scriban.Parsing;
using Scriban.Runtime;
using Zio;

namespace Lunet.Layouts
{
    public class LayoutProcessor : ContentProcessor<LayoutPlugin>
    {
        private readonly Dictionary<LayoutKey, LayoutContentObject> _layouts;

        private readonly List<KeyValuePair<string, GetLayoutPathsDelegate>> _layoutPathProviders;

        public static readonly ScriptVariableGlobal ContentVariable = new ScriptVariableGlobal("content");

        public const string LayoutFolderName = "layouts";

        public const string DefaultLayoutName = "_default";

        public delegate IEnumerable<UPath> GetLayoutPathsDelegate(SiteObject site, string layoutName, string layoutType);

        public LayoutProcessor(LayoutPlugin plugin) : base(plugin)
        {
            _layouts = new Dictionary<LayoutKey, LayoutContentObject>();
            _layoutPathProviders = new List<KeyValuePair<string, GetLayoutPathsDelegate>>();

            RegisterLayoutPathProvider(ContentLayoutTypes.Single, SingleLayout);
            RegisterLayoutPathProvider(ContentLayoutTypes.List, DefaultLayout);
        }

        public void RegisterLayoutPathProvider(string layoutType, GetLayoutPathsDelegate layoutPathsDelegate)
        {
            if (layoutType == null) throw new ArgumentNullException(nameof(layoutType));
            if (layoutPathsDelegate == null) throw new ArgumentNullException(nameof(layoutPathsDelegate));
            _layoutPathProviders.Add(new KeyValuePair<string, GetLayoutPathsDelegate>(layoutType, layoutPathsDelegate));
        }

        private GetLayoutPathsDelegate FindLayoutPaths(string layoutType)
        {
            foreach (var item in _layoutPathProviders)
            {
                if (item.Key == layoutType)
                {
                    return item.Value;
                }
            }
            
            // Always return a default layout
            return DefaultLayout;
        }

        public override ContentResult TryProcessContent(ContentObject page, ContentProcessingStage stage)
        {
            Debug.Assert(stage == ContentProcessingStage.Processing);
            
            if (page.ScriptObjectLocal == null)
            {
                return ContentResult.None;
            }

            var layoutName = page.Layout ?? page.Section;
            layoutName = NormalizeLayoutName(page, layoutName, true);
            var layoutType = page.LayoutType;
            var layoutNames = new HashSet<string>() {layoutName};
            var result = ContentResult.Continue;

            // For a list rendering the pages is setup
            if (layoutType == ContentLayoutTypes.List)
            {
                page.ScriptObjectLocal.SetValue("pages", Site.Pages, true);
            }
            
            bool continueLayout;
            do
            {
                continueLayout = false;
                // TODO: We are using content type here with the layout extension, is it ok?
                var layoutObject = GetLayout(layoutName, layoutType, page.ContentType);

                // If we haven't found any layout, this is not an error, so we let the 
                // content as-is
                if (layoutObject == null)
                {
                    Site.Warning($"No layout found for content [{page.Url}] with layout name [{layoutName}] and type [{layoutType}]");
                    break;
                }

                // Add dependency to the layout file
                page.Dependencies.Add(new FileContentDependency(new FileEntry(Site.FileSystem, layoutObject.SourceFile.Path)));
                
                // Override playground object
                layoutObject.CopyToWithReadOnly(page.ScriptObjectLocal);

                layoutObject.ScriptObjectLocal?.CopyToWithReadOnly(page.ScriptObjectLocal);

                // Clear the layout object to make sure it is not changed when processing layout between pages
                page.ScriptObjectLocal.SetValue(PageVariables.Page, page, true);
                page.ScriptObjectLocal.SetValue(PageVariables.Content, page.Content, false);

                // We manage global locally here as we need to push the local variable ScriptVariable.BlockDelegate
                if (Site.Scripts.TryEvaluatePage(page, layoutObject.Script, layoutObject.SourceFile.Path, page.ScriptObjectLocal))
                {
                    var nextLayoutName = layoutObject.GetSafeValue<string>(PageVariables.Layout);
                    var nextLayoutType = layoutObject.GetSafeValue<string>(PageVariables.LayoutType);
                    var nextLayout = NormalizeLayoutName(layoutObject, nextLayoutName, false);
                    if (nextLayout != layoutName && nextLayout != null)
                    {
                        if (!layoutNames.Add(nextLayout))
                        {
                            Site.Error($"Invalid recursive layout `{nextLayout}` from script `{layoutObject.SourceFile.Path}`");
                            result = ContentResult.Break;
                            break;
                        }

                        layoutName = nextLayout;
                        layoutType = nextLayoutType ?? layoutType;
                        continueLayout = true;
                    }
                }

            } while (continueLayout);

            // The file has been correctly layout
            return result;
        }

        private LayoutContentObject GetLayout(string layoutName, string layoutType, ContentType contentType)
        {
            LayoutContentObject layoutObject;

            layoutType ??= ContentLayoutTypes.Single;
            var layoutKey = new LayoutKey(layoutName, layoutType, contentType);
            if (_layouts.TryGetValue(layoutKey, out layoutObject))
            {
                return layoutObject;
            }

            var layoutDelegate = FindLayoutPaths(layoutType);
            if (layoutDelegate != null)
            {
                // Get all possible extensions for the specific content type
                var extensions = Site.ContentTypes.GetExtensionsByContentType(contentType);
                var layoutRoot = UPath.Root / LayoutFolderName;
                foreach (var layoutPath in layoutDelegate(Site, layoutName, layoutType))
                {
                    foreach (var extension in extensions)
                    {
                        var fullLayoutPath = layoutRoot / layoutPath.FullName + extension;
                        var entry = new FileEntry(Site.MetaFileSystem, fullLayoutPath);
                        if (entry.Exists)
                        {
                            var scriptLayoutText = entry.ReadAllText();
                            var scriptInstance = Site.Scripts.ParseScript(scriptLayoutText, fullLayoutPath, ScriptMode.FrontMatterAndContent);

                            if (scriptInstance.HasErrors)
                            {
                                goto exit;
                            }

                            layoutObject = new LayoutContentObject(Site, entry, scriptInstance);
                            
                            // We run first the front matter on the layout
                            if (layoutObject.FrontMatter != null && !Site.Scripts.TryRunFrontMatter(layoutObject.FrontMatter, layoutObject))
                            {
                                goto exit;
                            }

                            _layouts.Add(layoutKey, layoutObject);
                            return layoutObject;
                        }
                    }
                }
            }

            exit:
            return null;
        }

        private static IEnumerable<UPath> SingleLayout(SiteObject site, string layoutName, string layoutType)
        {
            // try: _meta/layouts/{layoutName}/single.{layoutExtension}
            yield return (UPath)layoutName / layoutType;

            // try: _meta/layouts/{layoutName}.{layoutExtension}
            yield return layoutName + "." + layoutType;
            
            // try: _meta/layouts/{layoutName}.{layoutExtension}
            yield return layoutName;

            if (layoutName != DefaultLayoutName)
            {
                // try: _meta/layouts/_default/single.{layoutExtension}
                yield return (UPath)DefaultLayoutName / layoutType;

                // try: _meta/layouts/_default.{layoutExtension}
                yield return (DefaultLayoutName);
            }
        }

        private static IEnumerable<UPath> DefaultLayout(SiteObject site, string layoutName, string layoutType)
        {
            // try: _meta/layouts/{layoutName}/list.{layoutExtension}
            yield return (UPath)layoutName / layoutType;

            // try: _meta/layouts/{layoutName}.list.{layoutExtension}
            yield return layoutName + "." + layoutType;

            if (layoutName != DefaultLayoutName)
            {
                // try: _meta/layouts/_default/list.{layoutExtension}
                yield return (UPath)DefaultLayoutName / (layoutType);

                // try: _meta/layouts/_default.list.{layoutExtension}
                yield return (DefaultLayoutName + "." + layoutType);
            }
        }

        private static readonly char[] InvalidLayoutChars = new[] {'\\', '/', '.'};

        private string NormalizeLayoutName(TemplateObject context, string layoutName, bool defaultIfNull)
        {
            if (string.IsNullOrEmpty(layoutName))
            {
                return defaultIfNull ? DefaultLayoutName : null;
            }
            layoutName = layoutName.Trim();
            if (layoutName.IndexOfAny(InvalidLayoutChars) >= 0)
            {
                Site.Warning($"In file {context.Path}, the layout `{layoutName}` contains invalid chars ({string.Join(", ", InvalidLayoutChars.Select(x => $"`{x}`"))}. Replacing invalid chars with `-`");
                foreach (var invalidLayoutChar in InvalidLayoutChars)
                {
                    layoutName = layoutName.Replace(invalidLayoutChar, '-');
                }
            }

            if (string.IsNullOrEmpty(layoutName))
            {
                return defaultIfNull ? DefaultLayoutName : null;
            }

            return layoutName;
        }

        struct LayoutKey : IEquatable<LayoutKey>
        {
            public LayoutKey(string name, string type, ContentType contentType)
            {
                Name = name;
                Type = type;
                ContentType = contentType;
            }

            public string Name;

            public string Type;

            public ContentType ContentType;

            public bool Equals(LayoutKey other)
            {
                return string.Equals(Name, other.Name) && string.Equals(Type, other.Type) && ContentType.Equals(other.ContentType);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is LayoutKey && Equals((LayoutKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name.GetHashCode();
                    hashCode = (hashCode * 397) ^ Type.GetHashCode();
                    hashCode = (hashCode * 397) ^ ContentType.GetHashCode();
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"{nameof(Name)}: {Name}, {nameof(Type)}: {Type}, {nameof(ContentType)}: {ContentType}";
            }
        }


    }

    [DebuggerDisplay("Layout: {" + nameof(Path) + "}")]
    public class LayoutContentObject : TemplateObject
    {
        public LayoutContentObject(SiteObject site, FileEntry sourceFileInfo, ScriptInstance scriptInstance) : base(site, ContentObjectType.File, sourceFileInfo, scriptInstance)
        {
            ScriptObjectLocal = new ScriptObject();
        }
    }
}