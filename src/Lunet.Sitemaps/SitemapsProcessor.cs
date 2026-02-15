// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Sitemaps;

public class SitemapsProcessor : ContentProcessor<SitemapsPlugin>
{
    private readonly ScriptCollection _urls;

    public const string DefaultUrl = "/sitemap.xml";

    public SitemapsProcessor(SitemapsPlugin plugin) : base(plugin)
    {
        _urls = new ScriptCollection();
        Site.Content.LayoutTypes.AddListType("sitemap");
    }

    public override void Process(ProcessingStage stage)
    {
        if (!Plugin.Enable) return;

        if (stage == ProcessingStage.BeforeLoadingContent)
        {
            _urls.Clear();
        }
        else if (stage == ProcessingStage.BeforeProcessingContent)
        {
            // Generate sitemap.xml
            ContentObject sitemapContent;
            if (Site.Content.Finder.TryFindByPath(DefaultUrl, out var existingSitemapContent) && existingSitemapContent is not null)
            {
                sitemapContent = existingSitemapContent;
                sitemapContent.ScriptObjectLocal ??= new ScriptObject();
            }
            else
            {
                sitemapContent = new DynamicContentObject(Site, DefaultUrl)
                {
                    ContentType = ContentType.Xml,
                    ScriptObjectLocal = new ScriptObject(), // Force the layout to participate
                };
                Site.DynamicPages.Add(sitemapContent);
            }
                
            sitemapContent.LayoutType ??= "sitemap";
            sitemapContent.ScriptObjectLocal["urls"] = _urls;

            if (!Site.Content.Finder.TryFindByPath("/robots.txt", out _))
            {
                // Generate robots.txt
                var robotsContent = new DynamicContentObject(Site, "/robots.txt")
                {
                    ContentType = ContentType.Txt,
                    LayoutType = ContentLayoutTypes.List,
                    Content = $"Sitemap: {Site.Content.Finder.UrlRef(null, sitemapContent.Url ?? DefaultUrl)}"
                };
                Site.DynamicPages.Add(robotsContent);
            }
        }
    }

    public override ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage)
    {
        var contentType = file.ContentType;
        if (!Plugin.Enable || !contentType.IsHtmlLike())
        {
            return ContentResult.Continue;
        }

        // if sitemap is false, then don't index it
        var allowSiteMap = file[SitemapPageVariables.Sitemap] is bool v ? v : true;
        if (!allowSiteMap) return ContentResult.Continue;
            
        var url = Site.Content.Finder.UrlRef(null, file.Url ?? "/");

        var sitemapUrl = new SitemapUrl(url)
        {
            LastModified = file.ModifiedTime.Ticks == 0 ? DateTime.Now : file.ModifiedTime.DateTime,
        };

        if (file.ContainsKey(SitemapPageVariables.SitemapPriority))
        {
            sitemapUrl.SetValue("priority", file.GetSafeValue<float>(SitemapPageVariables.SitemapPriority));
        }
            
        if (file.ContainsKey(SitemapPageVariables.SitemapChangeFrequency))
        {
            sitemapUrl.SetValue("changefreq", file.GetSafeValue<string>(SitemapPageVariables.SitemapChangeFrequency));
        }

        lock (_urls)
        {
            _urls.Add(sitemapUrl);
        }

        return ContentResult.Continue;
    }
}
