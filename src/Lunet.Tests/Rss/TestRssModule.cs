// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Rss;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Rss;

public class TestRssModule
{
    [Test]
    public void TestRssPluginRegistersSiteValueAndDefaults()
    {
        using var context = new SiteTestContext();
        var plugin = new RssPlugin(context.Site);

        Assert.AreSame(plugin, context.Site.GetSafeValue<RssPlugin>("rss"));
        Assert.AreEqual(10, plugin.Limit);
        Assert.IsTrue(context.Site.Content.LayoutTypes.ContainsKey("rss"));
    }

    [Test]
    public void TestRssLimitCanBeUpdated()
    {
        using var context = new SiteTestContext();
        var plugin = new RssPlugin(context.Site);

        plugin.Limit = 25;

        Assert.AreEqual(25, plugin.Limit);
    }
}
