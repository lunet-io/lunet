// Copyright (c) Alexandre Mutel. All rights reserved.
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

namespace Lunet.Layouts;

public class LayoutProcessor : ContentProcessor<LayoutPlugin>
{
    private readonly Dictionary<LayoutKey, LayoutContentObject> _layouts;

    private readonly List<KeyValuePair<string, GetLayoutPathsDelegate>> _layoutPathProviders;

    private readonly Dictionary<ContentType, ILayoutConverter> _converters;

    public static readonly ScriptVariableGlobal ContentVariable = new ScriptVariableGlobal("content");

    public const string LayoutFolderName = "layouts";

    public const string DefaultLayoutName = "_default";

    public delegate IEnumerable<UPath> GetLayoutPathsDelegate(SiteObject site, string layoutName, string layoutType);

    public LayoutProcessor(LayoutPlugin plugin) : base(plugin)
    {
        _layouts = new Dictionary<LayoutKey, LayoutContentObject>();
        _layoutPathProviders = new List<KeyValuePair<string, GetLayoutPathsDelegate>>();
        _converters = new Dictionary<ContentType, ILayoutConverter>();

        RegisterLayoutPathProvider(ContentLayoutTypes.Single, SingleLayout);
        RegisterLayoutPathProvider(ContentLayoutTypes.List, DefaultLayout);
    }

    public void RegisterLayoutPathProvider(string layoutType, GetLayoutPathsDelegate layoutPathsDelegate)
    {
        if (layoutType == null) throw new ArgumentNullException(nameof(layoutType));
        if (layoutPathsDelegate == null) throw new ArgumentNullException(nameof(layoutPathsDelegate));
        _layoutPathProviders.Add(new KeyValuePair<string, GetLayoutPathsDelegate>(layoutType, layoutPathsDelegate));
    }

    public void RegisterConverter(ContentType contentType, ILayoutConverter converter)
    {
        _converters[contentType] = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public override ContentResult TryProcessContent(ContentObject page, ContentProcessingStage stage)
    {
        Debug.Assert(stage == ContentProcessingStage.Processing);
            
        if (page.ScriptObjectLocal == null)
        {
            return TryConvertContent(page, false) ? ContentResult.Continue : ContentResult.None;
        }

        var layoutName = page.Layout ?? page.Section;
        var layoutType = page.LayoutType;
        var layoutContentType = page.ContentType;
        layoutName = NormalizeLayoutName(page, layoutName, true);
        var layoutKey = GetLayoutKey(layoutName, layoutType, layoutContentType);
        var layoutKeys = new HashSet<string>()
        {
            layoutKey
        };
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
            var layoutObject = GetLayout(layoutName, layoutType, layoutContentType);

            // If we don't have a layout, try to convert the content
            if (layoutObject == null)
            {
                // Perform a content conversion if supported and we don't have any layout available
                var previousContentType = page.ContentType;
                if (TryConvertContent(page, true))
                {
                    layoutContentType = page.ContentType;

                    // If the conversion succeeded, we should have a different layout
                    // in that case, we can try to resolve the layout with the new extension
                    if (previousContentType != layoutContentType)
                    {
                        continueLayout = true;
                        continue;
                    }
                }

                // otherwise, we have an error
                Site.Error($"No layout found for content [{page.Url}] with layout name [{layoutName}] and type [{layoutType}] with extension `{layoutContentType}`");
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
                var nextLayoutName = layoutObject.GetSafeValue<string>(PageVariables.Layout) ?? layoutName;
                var nextLayoutType = layoutObject.GetSafeValue<string>(PageVariables.LayoutType) ?? layoutType;
                var nextRawLayoutContentType = layoutObject.GetSafeValue<string>(PageVariables.LayoutContentType);
                var nextLayoutContentType = nextRawLayoutContentType != null ? new ContentType(nextRawLayoutContentType) : layoutContentType;
                nextLayoutName = NormalizeLayoutName(layoutObject, nextLayoutName, false);
                if ((nextLayoutName != layoutName || nextLayoutType != layoutType || nextLayoutContentType != layoutContentType) && nextLayoutName != null)
                {
                    layoutKey = GetLayoutKey(nextLayoutName, nextLayoutType, nextLayoutContentType);

                    if (!layoutKeys.Add(layoutKey))
                    {
                        Site.Error($"Invalid recursive layout `{layoutKey.Replace("|", ", ")}` from script `{layoutObject.SourceFile.Path}`");
                        result = ContentResult.Break;
                        break;
                    }

                    layoutName = nextLayoutName;
                    layoutType = nextLayoutType;
                    layoutContentType = nextLayoutContentType;
                    continueLayout = true;
                }
            }

        } while (continueLayout);

        // The file has been correctly layout
        return result;
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
        
    private bool TryConvertContent(ContentObject page, bool forLayout)
    {
        // Perform a content conversion if supported and we don't have any layout available
        _converters.TryGetValue(page.ContentType, out var converter);

        // Certain converter should always convert their content by default if there are no layout
        // (e.g scss => css)
        if (converter != null && (forLayout || converter.ShouldConvertIfNoLayout))
        {
            var previousContentType = page.ContentType;
            // Convert the page to the desired output
            converter.Convert(page);

            return page.ContentType != previousContentType;
        }

        return false;
    }

    private static string GetLayoutKey(string layoutName, string layoutType, ContentType contentType)
    {
        return $"{layoutName}|{layoutType}|{contentType}";
    }

    private LayoutContentObject GetLayout(string layoutName, string layoutType, ContentType contentType)
    {
        lock (_layouts)
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
                        var entry = new FileSystemItem(Site.MetaFileSystem, fullLayoutPath, false);
                        if (entry.Exists())
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
            // No layout object
            _layouts.Add(layoutKey, null);
            return null;
        }

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
            if (defaultIfNull)
            {
                layoutName = Site.Layout ?? DefaultLayoutName;
            }
            else
            {
                return null;
            }
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
            return defaultIfNull ? (Site.Layout ?? DefaultLayoutName) : null;
        }

        return layoutName;
    }

    private struct LayoutKey : IEquatable<LayoutKey>
    {
        public LayoutKey(string name, string type, ContentType contentType)
        {
            Name = name;
            Type = type;
            ContentType = contentType;
        }

        public readonly string Name;

        public readonly string Type;

        public readonly ContentType ContentType;

        public bool Equals(LayoutKey other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal) && string.Equals(Type, other.Type, StringComparison.Ordinal) && ContentType.Equals(other.ContentType);
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
    public LayoutContentObject(SiteObject site, in FileSystemItem sourceFileInfo, ScriptInstance scriptInstance) : base(site, ContentObjectType.File, sourceFileInfo, scriptInstance)
    {
        ScriptObjectLocal = new ScriptObject();
    }
}