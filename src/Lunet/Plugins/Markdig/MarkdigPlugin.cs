// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Plugins;
using Lunet.Plugins.Markdig;
using Markdig;
using Scriban.Runtime;

// Register this plugin
[assembly: SitePlugin(typeof(MarkdigPlugin))]

namespace Lunet.Plugins.Markdig
{
    public class MarkdigPlugin : ContentProcessor, ISitePlugin
    {
        private const string PluginName = "markdig";

        private readonly ScriptObject markdigOptions;

        public MarkdigPlugin()
        {
            markdigOptions = new ScriptObject();
        }

        public override string Name => PluginName;

        protected override void InitializeCore()
        {
            Site.Plugins.Processors.AddIfNotAlready(this);
            Site.Plugins.DynamicObject.SetValue(PluginName, markdigOptions, true);
        }

        public override ContentResult TryProcess(ContentObject page)
        {
            var extension = page.ContentExtension;

            // This plugin is only working on files with a frontmatter and the markdown extension
            if (!(page.HasFrontMatter && (extension == ".md" || extension == ".markdown")))
            {
                return ContentResult.None;
            }

            var pipeline = new MarkdownPipeline();

            if (markdigOptions.Count == 0)
            {
                pipeline.UseAllExtensions();
            }
            else
            {
                // TODO: handle Markdig options
            }

            var html = Markdown.ToHtml(page.Content, pipeline);
            page.Content = html;
            page.ContentExtension = Site.GetSafeDefaultPageExtension();

            // Allow further processing of this page
            return ContentResult.Continue;
        }
    }
}