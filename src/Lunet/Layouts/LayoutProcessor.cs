// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Plugins;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Layouts
{
    public class LayoutProcessor : ContentProcessor
    {
        private readonly Dictionary<string, Template> layouts;

        private const string LayoutDirectoryName = "layouts";

        private const string DefaultLayoutName = "_default";

        internal LayoutProcessor()
        {
            layouts = new Dictionary<string, Template>();
        }

        public override string Name => "layout";

        public override ContentResult TryProcess(ContentObject page)
        {
            if (page.Script == null || page.ScriptObjectLocal == null)
            {
                return ContentResult.None;
            }

            var layoutName = page.Layout ?? page.Section;
            layoutName = NormalizeLayoutName(layoutName);

            var layoutNames = new HashSet<string>() {layoutName};

            var result = ContentResult.Continue;

            bool continueLayout;
            do
            {
                continueLayout = false;
                var layoutExtension = page.ContentExtension ?? Site.GetSafeDefaultPageExtension();
                var layoutScript = GetLayout(layoutName, page.ScriptObjectLocal.GetSafeValue<string>(PageVariables.LayoutType), layoutExtension);

                // If we haven't found any layout, this is not an error, so we let the 
                // content as-is
                if (layoutScript == null)
                {
                    break;
                }

                // If we had any errors, the page is invalid, so we can't process it
                if (layoutScript.HasErrors)
                {
                    result = ContentResult.Break;
                    break;
                }

                // We run first the front matter on the page
                if (!Site.Scripts.TryRunFrontMatter(layoutScript.Page, page.DynamicObject))
                {
                    result = ContentResult.Break;
                    break;
                }

                page.ScriptObjectLocal.SetValue(PageVariables.Page, page.DynamicObject, true);

                // We manage global locally here as we need to push the local variable ScriptVariable.BlockDelegate
                Site.Scripts.Context.PushGlobal(page.ScriptObjectLocal);
                try
                {
                    Site.Scripts.Context.SetValue(ScriptVariable.BlockDelegate, page.Content);

                    if (Site.Scripts.TryEvaluate(page, layoutScript.Page, layoutScript.SourceFilePath))
                    {
                        var nextLayout = NormalizeLayoutName(page.Layout);
                        if (nextLayout != layoutName)
                        {
                            if (layoutNames.Contains(nextLayout))
                            {
                                Site.Error($"Invalid recursive layout [{nextLayout}] from script [{Site.GetRelativePath(layoutScript.SourceFilePath)}");
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
                    Site.Scripts.Context.PopGlobal();
                }

            } while (continueLayout);

            // The file has been correctly layout
            return result;
        }

        private Template GetLayout(string layoutName, string layoutType, string layoutExtension)
        {
            Template layoutPage = null;

            var layoutKey = layoutName + (layoutType ?? "__no_layout_type__") + layoutExtension;

            if (!layouts.TryGetValue(layoutKey, out layoutPage))
            {
                var layoutPaths = new List<string>();
                foreach (var metaDir in Site.Meta.Directories)
                {
                    layoutPaths.Clear();

                    // try: _meta/layouts/{layoutName}/{layoutType}.{layoutExtension}
                    if (!string.IsNullOrEmpty(layoutType))
                    {
                        layoutPaths.Add(Path.Combine(metaDir, LayoutDirectoryName, layoutName, layoutType + layoutExtension));
                    }

                    // try: _meta/layouts/{layoutName}/single.{layoutExtension}
                    layoutPaths.Add(Path.Combine(metaDir, LayoutDirectoryName, layoutName, "single" + layoutExtension));

                    // try: _meta/layouts/{layoutName}.{layoutExtension}
                    layoutPaths.Add(Path.Combine(metaDir, LayoutDirectoryName, layoutName + layoutExtension));

                    if (layoutName != DefaultLayoutName)
                    {
                        // try: _meta/layouts/_default/{layoutType}.{layoutExtension}
                        if (!string.IsNullOrEmpty(layoutType))
                        {
                            layoutPaths.Add(Path.Combine(metaDir, LayoutDirectoryName, DefaultLayoutName,
                                layoutType + layoutExtension));
                        }

                        // try: _meta/layouts/_default/single.{layoutExtension}
                        layoutPaths.Add(Path.Combine(metaDir, LayoutDirectoryName, DefaultLayoutName,
                            "single" + layoutExtension));

                        // try: _meta/layouts/_default.{layoutExtension}
                        layoutPaths.Add(Path.Combine(metaDir, LayoutDirectoryName,
                            DefaultLayoutName + layoutExtension));
                    }

                    foreach (var layoutPath in layoutPaths)
                    {
                        if (File.Exists(layoutPath))
                        {
                            var scriptLayoutText = File.ReadAllText(layoutPath);
                            layoutPage = Site.Scripts.ParseScript(scriptLayoutText, layoutPath, ParsingMode.FrontMatter);
                            break;
                        }
                    }
                    if (layoutPage != null)
                    {
                        break;
                    }
                }
                layouts.Add(layoutKey, layoutPage);
            }

            return layoutPage;
        }

        private string NormalizeLayoutName(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
            {
                return DefaultLayoutName;
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
                return DefaultLayoutName;
            }

            return layoutName;
        }
    }
}