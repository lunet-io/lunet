// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Bundles;
using Lunet.Minifiers;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Minifiers;

public class TestMinifiersModule
{
    [Test]
    public void TestMinifierPluginRegistersIntoBundleProcessor()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var minifierPlugin = new MinifierPlugin(context.Site, bundlePlugin);

        Assert.NotNull(minifierPlugin);
        Assert.IsTrue(bundlePlugin.BundleProcessor.Minifiers.Contains<MinifierPlugin>());
    }

    [Test]
    public void TestMinifierPluginMinifiesJavaScript()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var minifierPlugin = new MinifierPlugin(context.Site, bundlePlugin);
        var input = "function test () { return 1 + 2; }";

        var result = minifierPlugin.Minify("js", input, "/scripts/app.js");

        Assert.AreNotEqual(input, result);
        StringAssert.Contains("function test()", result);
    }

    [Test]
    public void TestMinifierPluginReturnsOriginalForUnknownType()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var minifierPlugin = new MinifierPlugin(context.Site, bundlePlugin);
        var input = "keep me";

        var result = minifierPlugin.Minify("raw", input, null);

        Assert.AreEqual(input, result);
    }

    [Test]
    public void TestMinifierPluginReturnsOriginalOnInvalidJavaScript()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var minifierPlugin = new MinifierPlugin(context.Site, bundlePlugin);
        var input = "function broken( {";

        var result = minifierPlugin.Minify("js", input, "/scripts/broken.js");

        Assert.AreEqual(input, result);
    }
}
