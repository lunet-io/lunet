// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Layouts;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;

namespace Lunet.Tests.Layouts;

public class TestLayoutsModule
{
    [Test]
    public void TestLayoutPluginRegistersProcessor()
    {
        using var context = new SiteTestContext();

        var plugin = new LayoutPlugin(context.Site);

        Assert.NotNull(plugin.Processor);
        Assert.AreSame(plugin.Processor, context.Site.Content.ContentProcessors[0]);
    }

    [Test]
    public void TestLayoutProcessorRejectsInvalidRegistrations()
    {
        using var context = new SiteTestContext();
        var plugin = new LayoutPlugin(context.Site);

        Assert.Throws<ArgumentNullException>(() => plugin.Processor.RegisterLayoutPathProvider(null!, static (_, _, _) => Array.Empty<Zio.UPath>()));
        Assert.Throws<ArgumentNullException>(() => plugin.Processor.RegisterLayoutPathProvider("single", null!));
        Assert.Throws<ArgumentNullException>(() => plugin.Processor.RegisterConverter(ContentType.Html, null!));
    }

    [Test]
    public void TestLayoutProcessorAppliesLayoutTemplate()
    {
        using var context = new SiteTestContext();
        var plugin = new LayoutPlugin(context.Site);
        context.WriteInputFile("/.lunet/layouts/docs.sbn-html", "<article>{{content}}</article>");
        var page = context.CreateFileContentObject("/posts/post.md", "ignored");
        page.ScriptObjectLocal = new ScriptObject();
        page.ContentType = ContentType.Html;
        page.Layout = "docs";
        page.LayoutType = ContentLayoutTypes.Single;
        page.Content = "Hello Layout";

        var result = plugin.Processor.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.AreEqual("<article>Hello Layout</article>", page.Content);
    }

    [Test]
    public void TestLayoutProcessorConvertsWithoutLayoutWhenConverterAllowsIt()
    {
        using var context = new SiteTestContext();
        var plugin = new LayoutPlugin(context.Site);
        var page = context.CreateFileContentObject("/posts/file.custom", "content");
        page.Content = "raw";
        var sourceType = page.ContentType;
        var converter = new RecordingConverter(shouldConvertIfNoLayout: true, newContentType: ContentType.Html);
        plugin.Processor.RegisterConverter(sourceType, converter);

        var result = plugin.Processor.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.AreEqual(1, converter.CallCount);
        Assert.AreEqual(ContentType.Html, page.ContentType);
        Assert.AreEqual("converted:raw", page.Content);
    }

    [Test]
    public void TestLayoutProcessorUsesConverterThenAppliesLayout()
    {
        using var context = new SiteTestContext();
        var plugin = new LayoutPlugin(context.Site);
        context.WriteInputFile("/.lunet/layouts/docs.sbn-html", "<div>{{content}}</div>");
        var page = context.CreateFileContentObject("/posts/file.input", "content");
        page.ScriptObjectLocal = new ScriptObject();
        page.Layout = "docs";
        page.LayoutType = ContentLayoutTypes.Single;
        page.Content = "to-convert";
        var sourceType = page.ContentType;
        var converter = new RecordingConverter(shouldConvertIfNoLayout: false, newContentType: ContentType.Html);
        plugin.Processor.RegisterConverter(sourceType, converter);

        var result = plugin.Processor.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Continue, result);
        Assert.AreEqual(1, converter.CallCount);
        Assert.AreEqual("<div>converted:to-convert</div>", page.Content);
        Assert.AreEqual(ContentType.Html, page.ContentType);
    }

    [Test]
    public void TestLayoutProcessorBreaksOnRecursiveLayouts()
    {
        using var context = new SiteTestContext();
        var plugin = new LayoutPlugin(context.Site);
        context.WriteInputFile(
            "/.lunet/layouts/a.sbn-html",
            """
            +++
            layout = "b"
            +++
            {{content}}
            """
        );
        context.WriteInputFile(
            "/.lunet/layouts/b.sbn-html",
            """
            +++
            layout = "a"
            +++
            {{content}}
            """
        );

        var page = context.CreateFileContentObject("/posts/recursive.md", "content");
        page.ScriptObjectLocal = new ScriptObject();
        page.ContentType = ContentType.Html;
        page.Layout = "a";
        page.LayoutType = ContentLayoutTypes.Single;
        page.Content = "test";

        var result = plugin.Processor.TryProcessContent(page, ContentProcessingStage.Processing);

        Assert.AreEqual(ContentResult.Break, result);
        Assert.IsTrue(context.Configuration.LoggerFactory.HasErrors);
    }

    private sealed class RecordingConverter : ILayoutConverter
    {
        private readonly ContentType _newContentType;

        public RecordingConverter(bool shouldConvertIfNoLayout, ContentType newContentType)
        {
            ShouldConvertIfNoLayout = shouldConvertIfNoLayout;
            _newContentType = newContentType;
        }

        public int CallCount { get; private set; }

        public bool ShouldConvertIfNoLayout { get; }

        public void Convert(ContentObject page)
        {
            CallCount++;
            page.Content = $"converted:{page.Content}";
            page.ChangeContentType(_newContentType);
        }
    }
}
