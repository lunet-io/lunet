// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Plugins;
using Lunet.Runtime;
using Textamina.Scriban;
using Textamina.Scriban.Parsing;
using Textamina.Scriban.Runtime;

namespace Lunet.Layouts
{
    public class LayoutProcessor : PageProcessorBase
    {
        private readonly Dictionary<string, Template> layouts;

        private const string LayoutDirectoryName = "layouts";

        private const string DefaultLayoutName = "_default";

        internal LayoutProcessor()
        {
            layouts = new Dictionary<string, Template>();
        }

        public override string Name => "layout";

        public override PageProcessResult TryProcess(ContentObject page)
        {
            if (page.Script == null || page.ScriptObject == null)
            {
                return PageProcessResult.None;
            }

            var layoutName = page.Layout ?? page.Section;
            layoutName = NormalizeLayoutName(layoutName);

            var layoutNames = new HashSet<string>() {layoutName};

            var result = PageProcessResult.Continue;

            bool continueLayout;
            do
            {
                continueLayout = false;
                var layoutExtension = page.ContentExtension ?? Site.GetSafeDefaultPageExtension();
                var scriptTemplate = GetLayout(layoutName, page.GetSafe<string>(PageVariables.LayoutType), layoutExtension);

                // If we haven't found any layout, this is not an error, so we let the 
                // content as-is
                if (scriptTemplate == null)
                {
                    break;
                }

                // If we had any errors, the page is invalid, so we can't process it
                if (scriptTemplate.HasErrors)
                {
                    result = PageProcessResult.Break;
                    break;
                }

                // We run first the front matter on the page
                if (!Site.Scripts.TryRunFrontMatter(scriptTemplate.Page, page))
                {
                    result = PageProcessResult.Break;
                    break;
                }

                page.ScriptObject.SetValue(PageVariables.Page, page, true);

                // We manage global locally here as we need to push the local variable ScriptVariable.BlockDelegate
                Site.Scripts.Context.PushGlobal(page.ScriptObject);
                try
                {
                    Site.Scripts.Context.SetValue(ScriptVariable.BlockDelegate, page.Content);

                    if (Site.Scripts.TryEvaluate(page, scriptTemplate.Page, scriptTemplate.SourceFilePath))
                    {
                        var nextLayout = NormalizeLayoutName(page.Layout);
                        if (nextLayout != layoutName)
                        {
                            if (layoutNames.Contains(nextLayout))
                            {
                                Site.Error($"Invalid recursive layout [{nextLayout}] from script [{Site.GetRelativePath(scriptTemplate.SourceFilePath)}");
                                result = PageProcessResult.Break;
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
                        layoutPaths.Add(Path.Combine(metaDir.FullName, LayoutDirectoryName, layoutName, layoutType + layoutExtension));
                    }

                    // try: _meta/layouts/{layoutName}/single.{layoutExtension}
                    layoutPaths.Add(Path.Combine(metaDir.FullName, LayoutDirectoryName, layoutName, "single" + layoutExtension));

                    // try: _meta/layouts/{layoutName}.{layoutExtension}
                    layoutPaths.Add(Path.Combine(metaDir.FullName, LayoutDirectoryName, layoutName + layoutExtension));

                    if (layoutName != DefaultLayoutName)
                    {
                        // try: _meta/layouts/_default/{layoutType}.{layoutExtension}
                        if (!string.IsNullOrEmpty(layoutType))
                        {
                            layoutPaths.Add(Path.Combine(metaDir.FullName, LayoutDirectoryName, DefaultLayoutName,
                                layoutType + layoutExtension));
                        }

                        // try: _meta/layouts/_default/single.{layoutExtension}
                        layoutPaths.Add(Path.Combine(metaDir.FullName, LayoutDirectoryName, DefaultLayoutName,
                            "single" + layoutExtension));

                        // try: _meta/layouts/_default.{layoutExtension}
                        layoutPaths.Add(Path.Combine(metaDir.FullName, LayoutDirectoryName,
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