// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Cards;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Cards;

public class TestCardsModule
{
    [Test]
    public void TestCardsPluginRegistersSiteValueAndHeadInclude()
    {
        using var context = new SiteTestContext();
        var plugin = new CardsPlugin(context.Site);

        Assert.AreSame(plugin, context.Site.GetSafeValue<CardsPlugin>("cards"));
        Assert.NotNull(plugin.Twitter);
        Assert.NotNull(plugin.Og);
        Assert.AreEqual("article", plugin.Og.Type);
        CollectionAssert.Contains(context.Site.Html.Head.Includes, "_builtins/cards.sbn-html");
    }

    [Test]
    public void TestCardsBasePropertiesCanBeSet()
    {
        using var context = new SiteTestContext();
        var plugin = new CardsPlugin(context.Site);

        plugin.Twitter.Enable = true;
        plugin.Twitter.Title = "Twitter title";
        plugin.Twitter.Description = "Twitter description";
        plugin.Twitter.Image = "/images/twitter.png";
        plugin.Twitter.ImageAlt = "Twitter image alt";
        plugin.Twitter.Card = "summary_large_image";
        plugin.Twitter.User = "@xenoatom";

        Assert.IsTrue(plugin.Twitter.Enable);
        Assert.AreEqual("Twitter title", plugin.Twitter.Title);
        Assert.AreEqual("Twitter description", plugin.Twitter.Description);
        Assert.AreEqual("/images/twitter.png", plugin.Twitter.Image);
        Assert.AreEqual("Twitter image alt", plugin.Twitter.ImageAlt);
        Assert.AreEqual("summary_large_image", plugin.Twitter.Card);
        Assert.AreEqual("@xenoatom", plugin.Twitter.User);
    }

    [Test]
    public void TestOpenGraphPropertiesCanBeSet()
    {
        using var context = new SiteTestContext();
        var plugin = new CardsPlugin(context.Site);

        plugin.Og.Enable = true;
        plugin.Og.Title = "Og title";
        plugin.Og.Description = "Og description";
        plugin.Og.Image = "/images/og.png";
        plugin.Og.ImageAlt = "Og image alt";
        plugin.Og.Type = "website";
        plugin.Og.Url = "https://example.com/docs";
        plugin.Og.Locale = "en_US";

        Assert.IsTrue(plugin.Og.Enable);
        Assert.AreEqual("Og title", plugin.Og.Title);
        Assert.AreEqual("Og description", plugin.Og.Description);
        Assert.AreEqual("/images/og.png", plugin.Og.Image);
        Assert.AreEqual("Og image alt", plugin.Og.ImageAlt);
        Assert.AreEqual("website", plugin.Og.Type);
        Assert.AreEqual("https://example.com/docs", plugin.Og.Url);
        Assert.AreEqual("en_US", plugin.Og.Locale);
    }
}
