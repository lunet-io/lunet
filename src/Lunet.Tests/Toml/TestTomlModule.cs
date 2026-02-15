// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Datas;
using Lunet.Tests.Infrastructure;
using Lunet.Toml;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Toml;

public class TestTomlModule
{
    [Test]
    public void TestTomlPluginRegistersDataLoader()
    {
        using var context = new SiteTestContext();
        var datasPlugin = new DatasPlugin(context.Site);
        var initialLoaderCount = datasPlugin.DataLoaders.Count;

        _ = new TomlPlugin(context.Site, datasPlugin);

        Assert.AreEqual(initialLoaderCount + 1, datasPlugin.DataLoaders.Count);
        Assert.IsInstanceOf<TomlDataLoader>(datasPlugin.DataLoaders[^1]);
    }

    [Test]
    public void TestTomlDataLoaderCanHandleExpectedExtension()
    {
        var loader = new TomlDataLoader();

        Assert.IsTrue(loader.CanHandle(".toml"));
        Assert.IsTrue(loader.CanHandle(".TOML"));
        Assert.IsFalse(loader.CanHandle(".json"));
    }

    [Test]
    public void TestTomlDataLoaderLoadsScriptObject()
    {
        var loader = new TomlDataLoader();
        using var fs = new MemoryFileSystem();
        var file = new FileEntry(fs, "/data.toml");
        file.WriteAllText(
            """
            title = "lunet"
            enabled = true
            count = 3
            """
        );

        var result = loader.Load(file) as ScriptObject;

        Assert.NotNull(result);
        Assert.AreEqual("lunet", result!["title"]);
        Assert.AreEqual(true, result["enabled"]);
        Assert.AreEqual(3L, result["count"]);
    }

    [Test]
    public void TestTomlUtilParsesNestedTablesAndArrays()
    {
        var text =
            """
            [site]
            title = "Lunet"

            tags = ["docs", "generator"]
            publish_date = 2024-01-01T12:30:00Z
            """;

        var result = TomlUtil.FromText(text) as ScriptObject;

        Assert.NotNull(result);
        var site = result!["site"] as ScriptObject;
        Assert.NotNull(site);
        Assert.AreEqual("Lunet", site!["title"]);
        var tags = site["tags"] as ScriptArray;
        Assert.NotNull(tags);
        CollectionAssert.AreEqual(new object[] { "docs", "generator" }, tags!);
        var publishDate = site["publish_date"];
        Assert.IsTrue(publishDate is DateTime || publishDate is DateTimeOffset);
    }

    [Test]
    public void TestTomlUtilThrowsLunetExceptionOnInvalidToml()
    {
        var ex = Assert.Throws<LunetException>(() => TomlUtil.FromText("a = [", "/broken.toml"));

        StringAssert.Contains("Error while parsing /broken.toml", ex!.Message);
    }
}
