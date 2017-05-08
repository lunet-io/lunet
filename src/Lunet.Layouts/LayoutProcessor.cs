// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Scriban;
using Scriban.Model;
using Scriban.Parsing;

namespace Lunet.Layouts
{
    public class LayoutProcessor : ContentProcessor<LayoutPlugin>
    {
        private readonly Dictionary<string, Template> _layouts;

        private readonly List<KeyValuePair<string, GetLayoutPathsDelegate>> _layoutPathProviders;

        public const string LayoutFolderName = "layouts";

        public const string DefaultLayoutName = "_default";

        public delegate IEnumerable<string> GetLayoutPathsDelegate(SiteObject site, string layoutName, string layoutType, string layoutExtension);

        public LayoutProcessor(LayoutPlugin plugin) : base(plugin)
        {
            _layouts = new Dictionary<string, Template>();
            _layoutPathProviders = new List<KeyValuePair<string, GetLayoutPathsDelegate>>();
            RegisterLayoutPathProvider(LayoutTypes.Single, SingleLayout);
            RegisterLayoutPathProvider(LayoutTypes.List, ListLayout);
        }

        public void RegisterLayoutPathProvider(string layoutType, GetLayoutPathsDelegate layoutPathsDelegate)
        {
            if (layoutType == null) throw new ArgumentNullException(nameof(layoutType));
            if (layoutPathsDelegate == null) throw new ArgumentNullException(nameof(layoutPathsDelegate));

            var existing = FindLayoutPaths(layoutType);
            if (existing != null)
            {
                throw new ArgumentException($"LayoutType [{layoutType}] cannot be registered multiple times", nameof(layoutType));
            }
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
            return null;
        }

        public override ContentResult TryProcess(ContentObject page)
        {
            if (page.ScriptObjectLocal == null)
            {
                return ContentResult.None;
            }

            var layoutName = page.Layout ?? page.Section;
            layoutName = NormalizeLayoutName(layoutName, true);

            var layoutNames = new HashSet<string>() {layoutName};

            var result = ContentResult.Continue;

            bool continueLayout;
            do
            {
                continueLayout = false;
                // TODO: We are using content type here with the layout extension, is it ok?
                var layoutExtension = PathUtil.NormalizeExtension(page.ContentType.Name) ?? Site.GetSafeDefaultPageExtension();
                var layoutScript = GetLayout(layoutName, page.LayoutType, layoutExtension);

                // If we haven't found any layout, this is not an error, so we let the 
                // content as-is
                if (layoutScript == null)
                {
                    Site.Warning($"No layout found for content [{page.Url}] with layout name [{layoutName}] and type [{page.LayoutType}]");
                    break;
                }

                // Add dependency to the layout file
                page.Dependencies.Add(new FileContentDependency(layoutScript.SourceFilePath));

                // If we had any errors, the page is invalid, so we can't process it
                if (layoutScript.HasErrors)
                {
                    result = ContentResult.Break;
                    break;
                }

                // We run first the front matter on the layout
                if (!Site.Scripts.TryRunFrontMatter(layoutScript.Page, page))
                {
                    result = ContentResult.Break;
                    break;
                }

                page.ScriptObjectLocal.SetValue(PageVariables.Page, page, true);

                // We manage global locally here as we need to push the local variable ScriptVariable.BlockDelegate
                var context = new TemplateContext(Site.Scripts.GlobalObject);
                context.PushGlobal(page.ScriptObjectLocal);
                try
                {
                    context.SetValue(ScriptVariable.BlockDelegate, page.Content);

                    if (Site.Scripts.TryEvaluate(page, layoutScript.Page, layoutScript.SourceFilePath, null, context))
                    {
                        var nextLayout = NormalizeLayoutName(page.Layout, false);
                        if (nextLayout != layoutName && nextLayout != null)
                        {
                            if (layoutNames.Contains(nextLayout))
                            {
                                Site.Error($"Invalid recursive layout [{nextLayout}] from script [{Site.GetRelativePath(layoutScript.SourceFilePath, PathFlags.File)}");
                                result = ContentResult.Break;
                                break;
                            }
                            layoutNames.Add(nextLayout);

                            layoutName = nextLayout;
                            continueLayout = true;
                        }
                    }
                }
                finally
                {
                    context.PopGlobal();
                }

            } while (continueLayout);

            // The file has been correctly layout
            return result;
        }

        private Template GetLayout(string layoutName, string layoutType, string layoutExtension)
        {
            Template layoutPage;

            layoutType = layoutType ?? LayoutTypes.Single;
            var layoutKey = layoutName + "/" + layoutType + layoutExtension;

            if (!_layouts.TryGetValue(layoutKey, out layoutPage))
            {
                var layoutDelegate = FindLayoutPaths(layoutType);

                if (layoutDelegate != null)
                {
                    foreach (var layoutPath in layoutDelegate(Site, layoutName, layoutType, layoutExtension))
                    {
                        if (File.Exists(layoutPath))
                        {
                            var scriptLayoutText = File.ReadAllText(layoutPath);
                            layoutPage = Site.Scripts.ParseScript(scriptLayoutText, layoutPath, ScriptMode.FrontMatterAndContent);
                            break;
                        }
                    }
                }
                _layouts.Add(layoutKey, layoutPage);
            }

            return layoutPage;
        }

        private static IEnumerable<string> SingleLayout(SiteObject site, string layoutName, string layoutType, string layoutExtension)
        {
            foreach (var metaDir in site.MetaFolders)
            {
                // try: _meta/layouts/{layoutName}/single.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutFolderName, layoutName, layoutType + layoutExtension);

                // try: _meta/layouts/{layoutName}.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutFolderName, layoutName + layoutExtension);

                if (layoutName != DefaultLayoutName)
                {
                    // try: _meta/layouts/_default/single.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutFolderName, DefaultLayoutName, layoutType + layoutExtension);

                    // try: _meta/layouts/_default.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutFolderName, DefaultLayoutName + layoutExtension);
                }
            }
        }

        private static IEnumerable<string> ListLayout(SiteObject site, string layoutName, string layoutType, string layoutExtension)
        {
            foreach (var metaDir in site.MetaFolders)
            {
                // try: _meta/layouts/{layoutName}/list.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutFolderName, layoutName, layoutType + layoutExtension);

                // try: _meta/layouts/{layoutName}.list.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutFolderName, layoutName + "." + layoutType + layoutExtension);

                if (layoutName != DefaultLayoutName)
                {
                    // try: _meta/layouts/_default/list.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutFolderName, DefaultLayoutName, layoutType + layoutExtension);

                    // try: _meta/layouts/_default.list.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutFolderName, DefaultLayoutName + "." + layoutType + layoutExtension);
                }
            }
        }

        private static string NormalizeLayoutName(string layoutName, bool defaultIfNull)
        {
            if (string.IsNullOrEmpty(layoutName))
            {
                return defaultIfNull ? DefaultLayoutName : null;
            }
            layoutName = layoutName.Trim();
            layoutName = layoutName.Replace('\\', '/');
            var index = layoutName.IndexOf('/');
            if (index > 0)
            {
                layoutName = layoutName.Substring(0, index);
            }
            else if (index == 0)
            {
                layoutName = null;
            }

            if (string.IsNullOrEmpty(layoutName))
            {
                return defaultIfNull ? DefaultLayoutName : null;
            }

            return layoutName;
        }
    }
}