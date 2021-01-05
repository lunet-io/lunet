// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using Lunet.Core;
using Lunet.Layouts;
using Lunet.Markdown.Extensions;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Scriban.Parsing;
using Scriban.Runtime;

// Register this plugin

namespace Lunet.Markdown
{
    public class MarkdownModule : SiteModule<MarkdownPlugin>
    {
    }

    public class MarkdownPlugin : SitePlugin, ILayoutConverter
    {
        private readonly DynamicObject<MarkdownPlugin> _markdigOptions;
        private readonly DynamicObject<MarkdownPlugin> _markdownHelper;

        public MarkdownPlugin(SiteObject site, LayoutPlugin layoutPlugin) : base(site)
        {
            _markdigOptions = new DynamicObject<MarkdownPlugin>(this);
            _markdownHelper = new DynamicObject<MarkdownPlugin>(this);

            _markdownHelper.SetValue("options", _markdigOptions, true);

            // Add a global markdown object 
            // with the markdown.to_html function
            Site.Scripts.Builtins.SetValue("markdown", _markdownHelper, true);
            _markdownHelper.Import("to_html", new Func<string, string>(ToHtmlFunction));

            // Register the markdown processor
            layoutPlugin.Processor.RegisterConverter(ContentType.Markdown, this);
        }

        public bool ShouldConvertIfNoLayout => false;

        public void Convert(ContentObject page)
        {
            var contentType = page.ContentType;

            // This converter is only working on files with a frontmatter and the markdown extension
            if (contentType != ContentType.Markdown)
            {
                return;
            }

            var html = ToHtml(page, GetPipeline());
            page.Content = html;
            page.ChangeContentType(ContentType.Html);
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
                else if (inline is AutolinkInline { IsEmail: false } autoLink)
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
                        var sourceSpan = new Scriban.Parsing.SourceSpan((string)page.Path, new TextPosition(span.Start, inline.Line, inline.Column), new TextPosition(span.End, inline.Line, inline.Column));
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