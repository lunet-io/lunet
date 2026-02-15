// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Core;

public class TestCoreSiteAndContentObjects
{
    [Test]
    public void TestSiteObjectDefaultValues()
    {
        using var context = new SiteTestContext();
        var site = context.Site;

        Assert.AreEqual("prod", site.Environment);
        Assert.AreEqual("/404.html", site.ErrorRedirect);
        Assert.AreEqual(".html", site.DefaultPageExtension);
        Assert.AreEqual(true, site.ReadmeAsIndex);
        Assert.NotNull(site.Builtins);
        Assert.NotNull(site.Html);
        Assert.AreEqual(3, site.Html.Head.Metas.Count);
        Assert.NotNull(site.Builtins.LunetObject.Version);
    }

    [Test]
    public void TestSiteObjectPathFiltering()
    {
        using var context = new SiteTestContext();
        var site = context.Site;

        Assert.IsFalse(site.IsHandlingPath("/config.scriban"));
        Assert.IsFalse(site.IsHandlingPath("/.lunet/build/www/index.html"));
        Assert.IsTrue(site.IsHandlingPath("/.lunet/includes/layout.sbn-html"));
        Assert.IsFalse(site.IsHandlingPath("/_drafts/post.md"));
    }

    [Test]
    public void TestSiteObjectAddDefineAndDefaultPageExtensionValidation()
    {
        using var context = new SiteTestContext();
        var site = context.Site;

        site.AddDefine("baseurl = \"https://example.com\"");
        Assert.AreEqual("https://example.com", site.BaseUrl);

        site.DefaultPageExtension = ".txt";
        Assert.AreEqual(".html", site.GetSafeDefaultPageExtension());
    }

    [Test]
    public void TestFileContentObjectReadmeResolvesToSectionFolder()
    {
        using var context = new SiteTestContext();
        var page = context.CreateFileContentObject("/docs/readme.md", "Hello");

        page.Initialize();

        Assert.AreEqual("docs", page.Section);
        Assert.AreEqual("/docs/", page.UrlWithoutBasePath);
        Assert.AreEqual("/docs/index.html", (string)page.GetDestinationPath());
    }

    [Test]
    public void TestFileContentObjectWithFrontMatterResolvesToDirectoryUrl()
    {
        using var context = new SiteTestContext();
        var page = context.CreateFileContentObject("/blog/my-post.md", "+++\n+++\nHello", withFrontMatterScript: true);

        page.Initialize();

        Assert.IsTrue(page.HasFrontMatter);
        Assert.AreEqual("/blog/my-post/", page.UrlWithoutBasePath);
        Assert.AreEqual("/blog/my-post/index.html", (string)page.GetDestinationPath());
    }

    [Test]
    public void TestFileContentObjectAppliesBasePath()
    {
        using var context = new SiteTestContext();
        context.Site.BasePath = "terminal";
        var page = context.CreateFileContentObject("/blog/my-post.md", "+++\n+++\nHello", withFrontMatterScript: true);

        page.Initialize();

        Assert.AreEqual("/blog/my-post/", page.UrlWithoutBasePath);
        Assert.AreEqual("/terminal/blog/my-post/", page.Url);
    }

    [Test]
    public void TestContentObjectChangeContentTypeUpdatesExtensions()
    {
        using var context = new SiteTestContext();
        var page = context.CreateFileContentObject("/docs/data.json", "{}");

        page.Initialize();
        page.ChangeContentType(ContentType.Xml);
        Assert.AreEqual("/docs/data.xml", page.UrlWithoutBasePath);

        page.ChangeContentType(ContentType.Html);
        Assert.AreEqual("/docs/data.html", page.UrlWithoutBasePath);
    }

    [Test]
    public void TestContentObjectUrlPlaceholdersAreExpanded()
    {
        using var context = new SiteTestContext();
        var page = context.CreateFileContentObject("/docs/my-post.md", "+++\n+++\nHello", withFrontMatterScript: true);
        page.Date = new DateTime(2024, 2, 3, 10, 30, 0);
        page.Title = "Hello Core";
        page.Url = "/archive/:year/:month/:slug/:section";

        page.Initialize();

        Assert.AreEqual("/archive/2024/02/hello-core/docs/", page.UrlWithoutBasePath);
    }

    [Test]
    public void TestPageCollectionOrderingByWeightAndDate()
    {
        using var context = new SiteTestContext();

        var first = context.CreateDynamicContentObject("first", path: "/posts/first.md");
        first.Weight = 20;
        first.Date = new DateTime(2024, 1, 1);
        first.Title = "B";

        var second = context.CreateDynamicContentObject("second", path: "/posts/second.md");
        second.Weight = 10;
        second.Date = new DateTime(2024, 1, 2);
        second.Title = "A";

        var third = context.CreateDynamicContentObject("third", path: "/posts/third.md");
        third.Weight = 20;
        third.Date = new DateTime(2024, 1, 3);
        third.Title = "C";

        var pages = new PageCollection { first, second, third };

        var byWeight = pages.OrderByWeight();
        Assert.AreSame(second, byWeight[0]);
        Assert.AreSame(third, byWeight[1]);
        Assert.AreSame(first, byWeight[2]);

        pages.Sort();
        Assert.AreSame(second, pages[0]);
        Assert.AreSame(third, pages[1]);
        Assert.AreSame(first, pages[2]);
    }

    [Test]
    public void TestDynamicContentObjectInitialization()
    {
        using var context = new SiteTestContext();
        context.Site.BasePath = "/terminal";

        var page = context.CreateDynamicContentObject("rss.xml", section: "feeds", path: "/feeds/rss.xml");
        page.ContentType = ContentType.Xml;

        Assert.AreEqual("feeds", page.Section);
        Assert.AreEqual("/terminal/rss.xml", page.Url);
        Assert.AreEqual("rss.xml", page.UrlWithoutBasePath);
    }
}
