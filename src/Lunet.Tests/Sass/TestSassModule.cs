// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Layouts;
using Lunet.Sass;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Sass;

public class TestSassModule
{
    [Test]
    public void TestSassPluginRegistersAliasesAndDefaults()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new SassPlugin(context.Site, layoutPlugin);

        Assert.AreSame(plugin, context.Site.GetSafeValue<SassPlugin>("scss"));
        Assert.AreSame(plugin, context.Site.GetSafeValue<SassPlugin>("sass"));
        Assert.NotNull(plugin.Includes);
        Assert.IsTrue(plugin.ShouldConvertIfNoLayout);
        Assert.IsFalse(plugin.UseDartSass);
    }

    [Test]
    public void TestSassConvertTransformsScssToCss()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new SassPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/styles/site.scss", "a { color: red; }");
        page.ContentType = SassPlugin.ScssType;

        plugin.Convert(page);

        Assert.AreEqual(ContentType.Css, page.ContentType);
        Assert.NotNull(page.Content);
        StringAssert.Contains("color", page.Content!);
        StringAssert.Contains("red", page.Content!);
    }

    [Test]
    public void TestSassConvertIgnoresNonScssFiles()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new SassPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/styles/site.css", "a { color: red; }");
        var previousType = page.ContentType;
        var previousContent = page.Content;

        plugin.Convert(page);

        Assert.AreEqual(previousType, page.ContentType);
        Assert.AreEqual(previousContent, page.Content);
    }

    [Test]
    public void TestLayoutProcessorConvertsScssWhenNoLayoutIsAvailable()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        _ = new SassPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/styles/auto.scss", "b { color: blue; }");
        page.ContentType = SassPlugin.ScssType;
        page.Initialize();

        var result = layoutPlugin.Processor.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.AreEqual(ContentType.Css, page.ContentType);
        StringAssert.Contains("blue", page.Content!);
    }
}
