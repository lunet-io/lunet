// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Markdig;
using Scriban.Runtime;

namespace Lunet.Markdown
{
    public class MarkdownProcessor : ContentProcessor<MarkdownPlugin>
    {
        private readonly DynamicObject<MarkdownProcessor> markdigOptions;
        private readonly DynamicObject<MarkdownProcessor> markdownHelper;

        public MarkdownProcessor(MarkdownPlugin plugin) : base(plugin)
        {
            markdigOptions = new DynamicObject<MarkdownProcessor>(this);
            markdownHelper = new DynamicObject<MarkdownProcessor>(this);

            markdownHelper.SetValue("options", markdigOptions, true);

            // Add a global markdown object 
            // with the markdown.to_html function
            Site.Scripts.GlobalObject.SetValue("markdown", markdownHelper, true);
            markdownHelper.Import("to_html", new Func<string, string>(ToHtmlFunction));
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
            var pipeline = new MarkdownPipelineBuilder();

            if (markdigOptions.Count == 0)
            {
                pipeline.UseAdvancedExtensions();
            }
            else
            {
                // TODO: handle Markdig options
            }
            return pipeline.Build();
        }

        private string ToHtmlFunction(string markdown)
        {
            var pipeline = GetPipeline();
            return Markdig.Markdown.ToHtml(markdown, pipeline);
        }
    }
}