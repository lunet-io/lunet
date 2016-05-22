// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Markdig;
using Scriban.Runtime;

namespace Lunet.Plugins.Markdig
{
    public class MarkdigProcessor : ContentProcessor
    {
        private readonly DynamicObject<MarkdigProcessor> markdigOptions;
        private readonly DynamicObject<MarkdigProcessor> markdownHelper;

        public MarkdigProcessor()
        {
            markdigOptions = new DynamicObject<MarkdigProcessor>(this);
            markdownHelper = new DynamicObject<MarkdigProcessor>(this);
        }

        protected override void InitializeCore()
        {
            Site.Plugins.SetValue("markdig", markdigOptions, true);

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