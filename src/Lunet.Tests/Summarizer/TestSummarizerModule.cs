// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Lunet.Core;
using Lunet.Layouts;
using Lunet.Summarizer;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Summarizer;

public class TestSummarizerModule
{
    [Test]
    public void TestSummarizerPluginRegistersBeforeLayoutProcessor()
    {
        using var context = new SiteTestContext();
        _ = new LayoutPlugin(context.Site);
        _ = new SummarizerPlugin(context.Site);

        var processors = context.Site.Content.ContentProcessors;
        var layoutProcessor = processors.Find<LayoutProcessor>();
        var summarizerProcessor = processors.Find<SummarizerProcessor>();

        Assert.NotNull(layoutProcessor);
        Assert.NotNull(summarizerProcessor);
        var layoutIndex = processors.IndexOf(layoutProcessor!);
        var summarizerIndex = processors.IndexOf(summarizerProcessor!);

        Assert.Less(summarizerIndex, layoutIndex);
    }

    [Test]
    public void TestSummarizerProcessorUsesMoreComment()
    {
        using var context = new SiteTestContext();
        var plugin = new SummarizerPlugin(context.Site);
        var page = context.CreateFileContentObject("/posts/more.html", "ignored");
        page.Content = "<p>intro section</p><!--more--><p>hidden section</p>";
        page.ContentType = ContentType.Html;

        var processor = context.Site.Content.ContentProcessors.Find<SummarizerProcessor>();
        var result = processor!.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.NotNull(page.Summary);
        StringAssert.Contains("intro section", page.Summary!);
        Assert.IsFalse(page.Summary!.Contains("hidden section", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void TestSummarizerProcessorRespectsSummaryWordCount()
    {
        using var context = new SiteTestContext();
        _ = new SummarizerPlugin(context.Site);
        var page = context.CreateFileContentObject("/posts/count.html", "ignored");
        page.Content = "<p>alpha beta gamma delta epsilon</p>";
        page.ContentType = ContentType.Html;
        page.SetValue(PageVariables.SummaryWordCount, 3);

        SummarizerHelper.UpdateSummary(page);

        Assert.NotNull(page.Summary);
        Assert.LessOrEqual(CountWords(page.Summary!), 3);
    }

    [Test]
    public void TestSummarizerProcessorUsesLunetSummarizeCommentAsStart()
    {
        using var context = new SiteTestContext();
        _ = new SummarizerPlugin(context.Site);
        var page = context.CreateFileContentObject("/posts/start.html", "ignored");
        page.Content = "<p>ignore this intro</p><!-- lunet:summarize --><p>keep this content for summary</p>";
        page.ContentType = ContentType.Html;
        page.SetValue(PageVariables.SummaryWordCount, 8);

        SummarizerHelper.UpdateSummary(page);

        Assert.NotNull(page.Summary);
        Assert.IsFalse(page.Summary!.Contains("ignore this intro", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains("keep this content", page.Summary!);
    }

    [Test]
    public void TestSummarizerProcessorIgnoresNonHtmlContent()
    {
        using var context = new SiteTestContext();
        _ = new SummarizerPlugin(context.Site);
        var page = context.CreateFileContentObject("/posts/readme.md", "ignored");
        page.Content = "# title";
        page.ContentType = ContentType.Markdown;

        var processor = context.Site.Content.ContentProcessors.Find<SummarizerProcessor>();
        var result = processor!.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.IsNull(page.Summary);
    }

    private static int CountWords(string text)
    {
        return Regex.Matches(text, @"\w+").Count;
    }
}
