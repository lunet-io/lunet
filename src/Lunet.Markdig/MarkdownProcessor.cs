// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using Lunet.Core;
using Lunet.Markdown.Extensions;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Scriban.Parsing;
using Scriban.Runtime;
using Zio;
using SourceSpan = Scriban.Parsing.SourceSpan;

namespace Lunet.Markdown
{
    public class MarkdownProcessor : ContentProcessor<MarkdownPlugin>
    {
        private readonly DynamicObject<MarkdownProcessor> _markdigOptions;
        private readonly DynamicObject<MarkdownProcessor> _markdownHelper;

        public MarkdownProcessor(MarkdownPlugin plugin) : base(plugin)
        {
            _markdigOptions = new DynamicObject<MarkdownProcessor>(this);
            _markdownHelper = new DynamicObject<MarkdownProcessor>(this);

            _markdownHelper.SetValue("options", _markdigOptions, true);

            // Add a global markdown object 
            // with the markdown.to_html function
            Site.Scripts.Builtins.SetValue("markdown", _markdownHelper, true);
            _markdownHelper.Import("to_html", new Func<string, string>(ToHtmlFunction));
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

            if (_markdigOptions.Count == 0)
            {
                pipeline.UseAdvancedExtensions();
            }
            else
            {
                // TODO: handle Markdig options
            }
            pipeline.Extensions.AddIfNotAlready<XRefMarkdownExtension>();
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

            foreach (var inline in markdownDocument.Descendants<Inline>())
            {
                string url;
                var span = inline.Span;

                if (inline is LinkInline link)
                {
                    url = link.Url;
                }
                else if (inline is AutolinkInline {IsEmail: false} autoLink)
                {
                    url = autoLink.Url;
                }
                else
                {
                    continue;
                }

                string label = null;
                
                if (string.IsNullOrEmpty(url)) continue;
                if (url.StartsWith("xref:"))
                {
                    var uid = url.Substring("xref:".Length);
                    if (Site.Content.Finder.TryFindByUid(uid, out var uidContent))
                    {
                        url = uidContent.Url;
                        label = uidContent.Title;
                    }
                    else
                    {
                        var sourceSpan = new Scriban.Parsing.SourceSpan((string) page.Path, new TextPosition(span.Start, inline.Line, inline.Column), new TextPosition(span.End, inline.Line, inline.Column));
                        Site.Warning(sourceSpan, $"Unable to find uid `{uid}` from xref");
                    }
                }
                
                var resolvedUrl = Site.Content.Finder.UrlRelRef(page, url);
                
                if (inline is LinkInline link1)
                {
                    if (label != null)
                    {
                        link1.AppendChild(new LiteralInline(label));
                    }
                    link1.Url = resolvedUrl;
                }
                else if (inline is AutolinkInline autoLink)
                {
                    autoLink.Url = resolvedUrl;
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