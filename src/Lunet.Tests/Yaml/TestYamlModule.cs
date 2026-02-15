// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Datas;
using Lunet.Tests.Infrastructure;
using Lunet.Yaml;
using Scriban;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Yaml;

public class TestYamlModule
{
    [Test]
    public void TestYamlPluginRegistersLoaderAndFrontMatterParser()
    {
        using var context = new SiteTestContext();
        var datasPlugin = new DatasPlugin(context.Site);
        var initialDataLoaderCount = datasPlugin.DataLoaders.Count;
        var initialFrontMatterParserCount = context.Site.Scripts.FrontMatterParsers.Count;

        _ = new YamlPlugin(context.Site, datasPlugin);

        Assert.AreEqual(initialDataLoaderCount + 1, datasPlugin.DataLoaders.Count);
        Assert.IsInstanceOf<YamlDataLoader>(datasPlugin.DataLoaders[^1]);
        Assert.AreEqual(initialFrontMatterParserCount + 1, context.Site.Scripts.FrontMatterParsers.Count);
        Assert.IsInstanceOf<YamlFrontMatterParser>(context.Site.Scripts.FrontMatterParsers[^1]);
    }

    [Test]
    public void TestYamlDataLoaderCanHandleExpectedExtensions()
    {
        var loader = new YamlDataLoader();

        Assert.IsTrue(loader.CanHandle(".yaml"));
        Assert.IsTrue(loader.CanHandle(".YML"));
        Assert.IsFalse(loader.CanHandle(".json"));
    }

    [Test]
    public void TestYamlDataLoaderLoadsScriptObject()
    {
        var loader = new YamlDataLoader();
        using var fs = new MemoryFileSystem();
        var file = new FileEntry(fs, "/site.yml");
        file.WriteAllText("name: lunet\nenabled: true\ncount: 4\n");

        var result = loader.Load(file) as ScriptObject;

        Assert.NotNull(result);
        Assert.AreEqual("lunet", result!["name"]);
        Assert.AreEqual(true, result["enabled"]);
        Assert.AreEqual(4, result["count"]);
    }

    [Test]
    public void TestYamlFrontMatterParserCanHandleSignatures()
    {
        var parser = new YamlFrontMatterParser();

        Assert.IsTrue(parser.CanHandle("---".ToCharArray()));
        Assert.IsTrue(parser.CanHandle(new byte[] { (byte)'-', (byte)'-', (byte)'-' }));
        Assert.IsFalse(parser.CanHandle("+++".ToCharArray()));
        Assert.IsFalse(parser.CanHandle(new byte[] { (byte)'+', (byte)'+', (byte)'+' }));
    }

    [Test]
    public void TestYamlFrontMatterParserParsesAndEvaluatesFrontMatter()
    {
        var parser = new YamlFrontMatterParser();
        var text = """
            ---
            title: My Post
            tags:
              - docs
              - lunet
            ---
            # Content
            """;

        var frontMatter = parser.TryParse(text, "/posts/a.md", out var position);

        Assert.NotNull(frontMatter);
        Assert.Greater(position.Offset, 0);
        Assert.AreEqual("# Content", text[position.Offset..].Trim());

        var templateContext = new TemplateContext();
        var globals = new ScriptObject();
        templateContext.PushGlobal(globals);
        frontMatter!.Evaluate(templateContext);

        Assert.AreEqual("My Post", globals["title"]);
        var tags = globals["tags"] as ScriptArray;
        Assert.NotNull(tags);
        CollectionAssert.AreEqual(new object[] { "docs", "lunet" }, tags!);
    }

    [Test]
    public void TestYamlFrontMatterParserReturnsNullWithoutYamlFrontMatter()
    {
        var parser = new YamlFrontMatterParser();
        var text = """
            title: plain text
            ---
            """;

        var frontMatter = parser.TryParse(text, "/posts/plain.md", out _);

        Assert.Null(frontMatter);
    }
}
