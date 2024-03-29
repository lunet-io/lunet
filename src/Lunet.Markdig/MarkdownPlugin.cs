// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Lunet.Core;
using Lunet.Layouts;
using Lunet.Markdown.Extensions;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;

// Register this plugin

namespace Lunet.Markdown;

public class MarkdownModule : SiteModule<MarkdownPlugin>
{
}

public class MarkdownPlugin : SitePlugin, ILayoutConverter
{
    private readonly MarkdownOptions _markdigOptions;
    private readonly DynamicObject<MarkdownPlugin> _markdownHelper;
    private readonly ThreadLocal<MarkdownPipeline> _markdownPipeline;

    public MarkdownPlugin(SiteObject site, LayoutPlugin layoutPlugin) : base(site)
    {
        _markdigOptions = new MarkdownOptions(this);
        _markdownHelper = new DynamicObject<MarkdownPlugin>(this);
        _markdownPipeline = new ThreadLocal<MarkdownPipeline>();

        _markdownHelper.SetValue("options", _markdigOptions, true);

        // Add a global markdown object 
        // with the markdown.to_html function
        Site.Builtins.SetValue("markdown", _markdownHelper, true);
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
        // Cache the pipeline per thread
        var pipeline = _markdownPipeline.Value;
        if (pipeline == null)
        {
            var builder = new MarkdownPipelineBuilder();

            switch (_markdigOptions.Extensions)
            {
                // TODO: Add support for other extensions.

                case "advanced":
                default:
                    builder.UseAdvancedExtensions();
                    break;
            }
            
            builder.Extensions.AddIfNotAlready<XRefMarkdownExtension>();
            pipeline = builder.Build();
            _markdownPipeline.Value = pipeline;
        }
        return pipeline;
    }
    private string ToHtmlFunction(string markdown)
    {
        var pipeline = GetPipeline();
        return Markdig.Markdown.ToHtml(markdown, pipeline);
    }

    private static readonly Regex ExtractXRef = new Regex(@"<xref\s*.*href=['""]([^'""]+)");

    private string ToHtml(ContentObject page, MarkdownPipeline pipeline)
    {
        if (page == null) throw new ArgumentNullException(nameof(page));
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

        var markdown = page.Content;
        var markdownDocument = Markdig.Markdown.Parse(markdown, pipeline);

        // Get css_img_attr
        string cssImgAttr = _markdigOptions.CssImageAttribute;
        var cssImgAttrParts = cssImgAttr is null ? Array.Empty<string>() : cssImgAttr.Split(',');
        
        foreach (var inline in markdownDocument.Descendants<Inline>())
        {
            string url = null;
            string label = null;

            bool isHandled = false;
            if (inline is LinkInline inputLink)
            {
                url = inputLink.Url;
                isHandled = true;
            }
            else if (inline is AutolinkInline { IsEmail: false } autoLink)
            {
                url = autoLink.Url;
                label = autoLink.Url;
                isHandled = true;
            }
            else if (inline is HtmlInline htmlInline)
            {
                if (htmlInline.Tag.StartsWith("<xref "))
                {
                    var urlMatch = ExtractXRef.Match(htmlInline.Tag);
                    label = urlMatch.Success ? urlMatch.Groups[1].Value : "INVALID";
                    var xref = Uri.UnescapeDataString(label);
                    url = $"xref:{xref}";
                    isHandled = true;
                }
                else if (htmlInline.Tag.StartsWith("</xref>"))
                {
                    htmlInline.Remove();
                }
            }

            if (!isHandled)
            {
                continue;
            }

            if (string.IsNullOrEmpty(url)) continue;
            var resolvedUrl = Site.Content.Finder.UrlRelRef(page, url);

            // Extract a more meaningful label
            bool isXref = url.StartsWith("xref:");
            if (isXref)
            {
                var uid = url.Substring("xref:".Length);
                Site.Content.Finder.TryGetTitleByUid(uid, out label);
            }

            var link = inline as LinkInline;
            if (link == null)
            {
                link = new LinkInline(resolvedUrl, label);
                inline.ReplaceBy(link);
            }

            if (label != null)
            {
                link.AppendChild(new LiteralInline(label));
            }
            link.Url = resolvedUrl;

            // Apply css_img_attr by adding class attribute to all images
            if (link.IsImage && cssImgAttrParts.Length > 0)
            {
                var attr = link.GetAttributes();
                foreach (var cssClass in cssImgAttrParts)
                {
                    attr.AddClass(cssClass);
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