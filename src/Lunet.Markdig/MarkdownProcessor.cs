// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using Lunet.Core;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Scriban.Runtime;
using Zio;

namespace Lunet.Markdown
{
    public class MarkdownProcessor : ContentProcessor<MarkdownPlugin>
    {
        private readonly DynamicObject<MarkdownProcessor> markdigOptions;
        private readonly DynamicObject<MarkdownProcessor> markdownHelper;
        private readonly Dictionary<UPath, ContentObject> _pages;

        public MarkdownProcessor(MarkdownPlugin plugin) : base(plugin)
        {
            markdigOptions = new DynamicObject<MarkdownProcessor>(this);
            markdownHelper = new DynamicObject<MarkdownProcessor>(this);
            _pages = new Dictionary<UPath, ContentObject>();

            markdownHelper.SetValue("options", markdigOptions, true);

            // Add a global markdown object 
            // with the markdown.to_html function
            Site.Scripts.Builtins.SetValue("markdown", markdownHelper, true);
            markdownHelper.Import("to_html", new Func<string, string>(ToHtmlFunction));
        }


        public override void Process(ProcessingStage stage)
        {
            if (stage == ProcessingStage.BeforeProcessingContent)
            {
                foreach (var page in Site.Pages)
                {
                    _pages[page.Path] = page;
                }
            }
        }

        public override ContentResult TryProcessContent(ContentObject page, ContentProcessingStage stage)
        {
            Debug.Assert(stage == ContentProcessingStage.Processing);

            var contentType = page.ContentType;

            // This plugin is only working on files with a frontmatter and the markdown extension
            if (!page.HasFrontMatter || contentType != ContentType.Markdown)
            {
                return ContentResult.None;
            }

            var html = ToHtml(page, GetPipeline());
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

        private string ToHtml(ContentObject page, MarkdownPipeline pipeline)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

            var markdown = page.Content;
            var markdownDocument = Markdig.Markdown.Parse(markdown, pipeline);

            var currentDir = page.Path.GetDirectory();
            
            foreach (var link in markdownDocument.Descendants<LinkInline>())
            {
                var url = link.Url;
                if (string.IsNullOrEmpty(url) || url.Contains(":") || url.StartsWith("/")) continue;


                var indexOfLastPart = url.IndexOf('?');
                if (indexOfLastPart < 0)
                {
                    indexOfLastPart = url.IndexOf('#');
                }

                string query = null;

                if (indexOfLastPart > 0)
                {
                    query = url.Substring(indexOfLastPart);
                    url = url.Substring(0, indexOfLastPart);
                }

                if (!UPath.TryParse(url, out var path))
                {
                    continue;
                }

                if (path.IsRelative)
                {
                    path = currentDir / path;
                }

                if (_pages.TryGetValue(path, out var pageLink))
                {
                    var destPath = pageLink.GetDestinationPath();

                    link.Url = (string) destPath;
                    if (link.Url.EndsWith("/index.html") || link.Url.EndsWith("/index.htm"))
                    {
                        link.Url = (string)destPath.GetDirectory();
                    }

                    // Append the query
                    if (query != null)
                    {
                        link.Url += query;
                    }
                }
            }

            var renderer = new HtmlRenderer(new StringWriter());
            pipeline.Setup(renderer);
            renderer.Render(markdownDocument);
            renderer.Writer.Flush();
            var str = renderer.Writer.ToString();
            return str;
        }
    }
}