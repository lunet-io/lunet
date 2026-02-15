// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using System.Text;
using Lunet.Core;
using Lunet.Datas;
using Lunet.Json;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Json;

public class TestJsonModule
{
    [Test]
    public void TestJsonPluginRegistersDataLoader()
    {
        using var context = new SiteTestContext();
        var datasPlugin = new DatasPlugin(context.Site);
        var initialLoaderCount = datasPlugin.DataLoaders.Count;

        _ = new JsonPlugin(context.Site, datasPlugin);

        Assert.AreEqual(initialLoaderCount + 1, datasPlugin.DataLoaders.Count);
        Assert.IsInstanceOf<JsonDataLoader>(datasPlugin.DataLoaders[^1]);
    }

    [Test]
    public void TestJsonDataLoaderCanHandleExpectedExtension()
    {
        var loader = new JsonDataLoader();

        Assert.IsTrue(loader.CanHandle(".json"));
        Assert.IsTrue(loader.CanHandle(".JSON"));
        Assert.IsFalse(loader.CanHandle(".yaml"));
    }

    [Test]
    public void TestJsonDataLoaderLoadsScriptObject()
    {
        var loader = new JsonDataLoader();
        using var fs = new MemoryFileSystem();
        var file = new FileEntry(fs, "/data.json");
        file.WriteAllText("""{ "name": "lunet", "enabled": true, "count": 12 }""");

        var result = loader.Load(file) as ScriptObject;

        Assert.NotNull(result);
        Assert.AreEqual("lunet", result!["name"]);
        Assert.AreEqual(true, result["enabled"]);
        Assert.AreEqual(12, result["count"]);
    }

    [Test]
    public void TestJsonUtilSupportsCommentsAndTrailingCommas()
    {
        var text = """
            {
              // comment
              "tags": [ "docs", "lunet", ],
              "size": 1.5,
            }
            """;

        var result = JsonUtil.FromText(text) as ScriptObject;

        Assert.NotNull(result);
        var tags = result!["tags"] as ScriptArray;
        Assert.NotNull(tags);
        CollectionAssert.AreEqual(new object[] { "docs", "lunet" }, tags!);
        Assert.AreEqual(1.5m, result["size"]);
    }

    [Test]
    public void TestJsonUtilFromStreamReadsObjects()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{ "value": 42 }"""));

        var result = JsonUtil.FromStream(stream) as ScriptObject;

        Assert.NotNull(result);
        Assert.AreEqual(42, result!["value"]);
    }

    [Test]
    public void TestJsonUtilThrowsLunetExceptionOnInvalidJson()
    {
        var ex = Assert.Throws<LunetException>(() => JsonUtil.FromText("{ invalid", "/broken.json"));

        StringAssert.Contains("Error while parsing /broken.json", ex!.Message);
    }
}
