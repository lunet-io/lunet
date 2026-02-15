// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Reflection;
using Lunet.Core;
using Lunet.Layouts;
using Lunet.Markdown;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Markdown;

public class TestMarkdownModule
{
    [Test]
    public void TestMarkdownPluginRegistersBuiltinsAndConverter()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);

        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);

        Assert.NotNull(plugin);
        Assert.IsFalse(plugin.ShouldConvertIfNoLayout);
        Assert.IsTrue(context.Site.Builtins.ContainsKey("markdown"));
    }

    [Test]
    public void TestMarkdownConvertChangesMarkdownToHtml()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/posts/post.md", "ignored");
        page.Content = "# Title";

        plugin.Convert(page);

        Assert.AreEqual(ContentType.Html, page.ContentType);
        StringAssert.Contains("<h1", page.Content!);
        StringAssert.Contains("Title", page.Content!);
    }

    [Test]
    public void TestMarkdownConvertIgnoresNonMarkdownContent()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/posts/page.html", "ignored");
        page.Content = "<h1>Hello</h1>";
        var previousContent = page.Content;
        var previousType = page.ContentType;

        plugin.Convert(page);

        Assert.AreEqual(previousType, page.ContentType);
        Assert.AreEqual(previousContent, page.Content);
    }

    [Test]
    public void TestMarkdownOptionsAffectImageCssAttributes()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);
        var options = GetOptions(plugin);
        options.CssImageAttribute = "img-fluid,rounded";

        var page = context.CreateFileContentObject("/posts/image.md", "ignored");
        page.Content = "![Alt](/image.png)";

        plugin.Convert(page);

        StringAssert.Contains("class=\"img-fluid rounded\"", page.Content!);
    }

    [Test]
    public void TestMarkdownConvertsStandardLinksToHtmlAnchors()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/posts/links.md", "ignored");
        page.Content = "[OpenAI](https://openai.com)";

        plugin.Convert(page);

        StringAssert.Contains("href=\"https://openai.com\"", page.Content!);
        StringAssert.Contains(">OpenAI<", page.Content!);
    }

    [Test]
    public void TestMarkdownUsesCustomAlertRenderer()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);
        var page = context.CreateFileContentObject("/posts/alert.md", "ignored");
        page.Content =
            """
            > [!NOTE]
            > Alert content
            """;

        plugin.Convert(page);

        StringAssert.Contains("lunet-alert-note", page.Content!);
        StringAssert.Contains("lunet-alert-note-content", page.Content!);
    }

    [Test]
    public void TestMarkdownToHtmlBuiltinFunctionWorks()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        var plugin = new MarkdownPlugin(context.Site, layoutPlugin);

        var method = typeof(MarkdownPlugin).GetMethod("ToHtmlFunction", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access MarkdownPlugin.ToHtmlFunction");
        }

        var result = method.Invoke(plugin, new object[] { "**bold**" }) as string;

        Assert.NotNull(result);
        StringAssert.Contains("<strong>bold</strong>", result!);
    }

    private static MarkdownOptions GetOptions(MarkdownPlugin plugin)
    {
        var field = typeof(MarkdownPlugin).GetField("_markdigOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
        {
            throw new InvalidOperationException("Unable to access MarkdownPlugin options");
        }

        return (MarkdownOptions)field.GetValue(plugin)!;
    }
}
