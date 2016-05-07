// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.IO;
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

        private readonly DynamicObject<MarkdigPlugin> markdigOptions;
        private readonly DynamicObject<MarkdigPlugin> markdownHelper;

        private delegate string ToHtmlDelegate(string markdown);

        public MarkdigPlugin()
        {
            markdigOptions = new DynamicObject<MarkdigPlugin>(this);
            markdownHelper = new DynamicObject<MarkdigPlugin>(this);
        }

        public override string Name => PluginName;

        protected override void InitializeCore()
        {
            Site.Plugins.Processors.AddIfNotAlready(this);

            Site.Plugins.SetValue(PluginName, markdigOptions, true);

            // Add a global markdown object 
            // with the markdown.to_html function
            Site.Scripts.GlobalObject.SetValue("markdown", markdownHelper, true);
            markdownHelper.Import("to_html", (ToHtmlDelegate)ToHtmlFunction);
        }

        public override ContentResult TryProcess(ContentObject page)
        {
            var contentType = page.ContentType;

            // This plugin is only working on files with a frontmatter and the markdown extension
            if (!page.HasFrontMatter || contentType != ContentType.Markdown)
            {
                return ContentResult.None;
            }

            var html = ToHtmlFunction(page.Content);
            page.Content = html;
            page.ChangeContentType(ContentType.Html);

            // Allow further processing of this page
            return ContentResult.Continue;
        }

        private MarkdownPipeline GetPipeline()
        {
            var pipeline = new MarkdownPipeline();

            if (markdigOptions.Count == 0)
            {
                pipeline.UseAllExtensions();
            }
            else
            {
                // TODO: handle Markdig options
            }
            return pipeline;
        }

        private string ToHtmlFunction(string markdown)
        {
            var pipeline = GetPipeline();
            return Markdown.ToHtml(markdown, pipeline);
        }
    }
}