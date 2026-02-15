// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Globalization;
using Lunet.Core;
using Lunet.Sitemaps;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;

namespace Lunet.Tests.Sitemaps;

public class TestSitemapsModule
{
    [Test]
    public void TestSitemapsPluginRegistersProcessorAndDefaultState()
    {
        using var context = new SiteTestContext();
        var plugin = new SitemapsPlugin(context.Site);

        Assert.IsTrue(plugin.Enable);
        Assert.NotNull(context.Site.Content.BeforeLoadingProcessors.Find<SitemapsProcessor>());
        Assert.NotNull(context.Site.Content.BeforeProcessingProcessors.Find<SitemapsProcessor>());
        Assert.NotNull(context.Site.Content.AfterRunningProcessors.Find<SitemapsProcessor>());
    }

    [Test]
    public void TestSitemapsProcessorCollectsUrlsAndGeneratesDynamicPages()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";
        var plugin = new SitemapsPlugin(context.Site);
        var processor = context.Site.Content.BeforeLoadingProcessors.Find<SitemapsProcessor>()!;
        processor.Process(ProcessingStage.BeforeLoadingContent);

        var page = context.CreateFileContentObject("/docs/page.html", "<h1>Hello</h1>");
        page.Initialize();
        page.SetValue(SitemapPageVariables.SitemapPriority, 0.8f);
        page.SetValue(SitemapPageVariables.SitemapChangeFrequency, "weekly");

        var result = processor.TryProcessContent(page, ContentProcessingStage.Running);
        processor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.AreEqual(2, context.Site.DynamicPages.Count);

        var sitemap = FindDynamicPageByUrl(context.Site.DynamicPages, SitemapsProcessor.DefaultUrl);
        Assert.NotNull(sitemap);
        Assert.AreEqual(ContentType.Xml, sitemap!.ContentType);
        Assert.AreEqual("sitemap", sitemap.LayoutType);

        var urls = sitemap.ScriptObjectLocal!["urls"] as ScriptCollection;
        Assert.NotNull(urls);
        Assert.AreEqual(1, urls!.Count);

        var sitemapUrl = urls[0] as SitemapUrl;
        Assert.NotNull(sitemapUrl);
        Assert.AreEqual("https://example.com/docs/page.html", sitemapUrl!.Url);
        Assert.NotNull(sitemapUrl["priority"]);
        Assert.AreEqual(0.8f, Convert.ToSingle(sitemapUrl["priority"], CultureInfo.InvariantCulture));
        Assert.AreEqual("weekly", sitemapUrl.ChangeFrequency);

        var robots = FindDynamicPageByUrl(context.Site.DynamicPages, "/robots.txt");
        Assert.NotNull(robots);
        StringAssert.Contains("Sitemap: https://example.com/sitemap.xml", robots!.Content!);
    }

    [Test]
    public void TestSitemapsProcessorSkipsWhenDisabled()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";
        var plugin = new SitemapsPlugin(context.Site)
        {
            Enable = false
        };
        var processor = context.Site.Content.BeforeLoadingProcessors.Find<SitemapsProcessor>()!;
        processor.Process(ProcessingStage.BeforeLoadingContent);

        var page = context.CreateFileContentObject("/docs/page.html", "<h1>Skip</h1>");
        page.Initialize();

        processor.TryProcessContent(page, ContentProcessingStage.Running);
        processor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(0, context.Site.DynamicPages.Count);
    }

    [Test]
    public void TestSitemapUrlPropertyRoundtrip()
    {
        var url = new SitemapUrl("https://example.com/docs/")
        {
            ChangeFrequency = "daily",
            LastModified = new DateTime(2025, 2, 3),
            Priority = 0.5f
        };

        Assert.AreEqual("https://example.com/docs/", url.Url);
        Assert.AreEqual("daily", url.ChangeFrequency);
        Assert.AreEqual(new DateTime(2025, 2, 3), url.LastModified);
        Assert.AreEqual(0.5f, url.Priority);
    }

    private static DynamicContentObject? FindDynamicPageByUrl(PageCollection pages, string expectedUrl)
    {
        foreach (var page in pages)
        {
            if (page is DynamicContentObject dynamicPage && dynamicPage.Url == expectedUrl)
            {
                return dynamicPage;
            }
        }

        return null;
    }
}
