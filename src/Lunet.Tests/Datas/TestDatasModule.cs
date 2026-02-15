// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Core;
using Lunet.Datas;
using Lunet.Tests.Infrastructure;
using Zio;

namespace Lunet.Tests.Datas;

public class TestDatasModule
{
    [Test]
    public void TestDatasPluginRegistersRootDataAndProcessor()
    {
        using var context = new PhysicalSiteTestContext();
        var plugin = new DatasPlugin(context.Site);

        Assert.AreSame(plugin.RootDataObject, context.Site["data"]);
        Assert.AreSame(plugin, plugin.RootDataObject.Parent);
        Assert.AreEqual(0, plugin.DataLoaders.Count);
        Assert.NotNull(context.Site.Content.BeforeInitializingProcessors.Find<DatasProcessor>());
    }

    [Test]
    public void TestDatasProcessorLoadsNestedDataFiles()
    {
        using var context = new PhysicalSiteTestContext();
        var plugin = new DatasPlugin(context.Site);
        plugin.DataLoaders.Add(new RecordingLoader(".txt", file => file.ReadAllText()));

        WriteMetaFile(context, "/data/users/alice.txt", "Alice");
        WriteMetaFile(context, "/data/users/bob.txt", "Bob");

        var processor = new DatasProcessor(plugin);
        processor.Process(ProcessingStage.BeforeInitializing);

        var users = plugin.RootDataObject["users"] as DataFolderObject;
        Assert.NotNull(users);
        Assert.AreEqual("Alice", users!["alice"]);
        Assert.AreEqual("Bob", users["bob"]);
    }

    [Test]
    public void TestDatasProcessorSkipsFilesWithoutMatchingLoader()
    {
        using var context = new PhysicalSiteTestContext();
        var plugin = new DatasPlugin(context.Site);
        plugin.DataLoaders.Add(new RecordingLoader(".txt", file => file.ReadAllText()));

        WriteMetaFile(context, "/data/users/alice.json", "{ \"name\": \"alice\" }");

        var processor = new DatasProcessor(plugin);
        processor.Process(ProcessingStage.BeforeInitializing);

        var users = plugin.RootDataObject["users"] as DataFolderObject;
        Assert.NotNull(users);
        Assert.IsFalse(users!.ContainsKey("alice"));
    }

    [Test]
    public void TestDatasProcessorHandlesDuplicateEntryNames()
    {
        using var context = new PhysicalSiteTestContext();
        var plugin = new DatasPlugin(context.Site);
        var txtLoader = new RecordingLoader(".txt", file => $"txt:{file.ReadAllText()}");
        var csvLoader = new RecordingLoader(".csv", file => $"csv:{file.ReadAllText()}");
        plugin.DataLoaders.Add(txtLoader);
        plugin.DataLoaders.Add(csvLoader);

        WriteMetaFile(context, "/data/items/product.txt", "A");
        WriteMetaFile(context, "/data/items/product.csv", "B");

        var processor = new DatasProcessor(plugin);
        processor.Process(ProcessingStage.BeforeInitializing);

        var items = plugin.RootDataObject["items"] as DataFolderObject;
        Assert.NotNull(items);
        Assert.IsTrue(items!.ContainsKey("product"));
        Assert.AreEqual(1, txtLoader.LoadCallCount);
        Assert.AreEqual(1, csvLoader.LoadCallCount);
    }

    [Test]
    public void TestDatasProcessorLogsErrorWhenLoaderThrows()
    {
        using var context = new PhysicalSiteTestContext();
        var plugin = new DatasPlugin(context.Site);
        plugin.DataLoaders.Add(new RecordingLoader(".txt", _ => throw new InvalidOperationException("Expected failure")));
        WriteMetaFile(context, "/data/users/alice.txt", "Alice");

        var processor = new DatasProcessor(plugin);
        processor.Process(ProcessingStage.BeforeInitializing);

        var users = plugin.RootDataObject["users"] as DataFolderObject;
        Assert.NotNull(users);
        Assert.IsFalse(users!.ContainsKey("alice"));
        Assert.IsTrue(context.Configuration.LoggerFactory.HasErrors);
    }

    [Test]
    public void TestDataFolderObjectToStringContainsFolderPath()
    {
        using var context = new PhysicalSiteTestContext();
        var plugin = new DatasPlugin(context.Site);
        var folder = new DirectoryEntry(context.Site.MetaFileSystem, "/data/docs");

        var dataFolder = new DataFolderObject(plugin, folder);

        Assert.AreEqual("DataFolder(/data/docs)", dataFolder.ToString());
    }

    private static void WriteMetaFile(PhysicalSiteTestContext context, string path, string content)
    {
        context.WriteMetaFile(path, content);
    }

    private sealed class RecordingLoader : IDataLoader
    {
        private readonly string _extension;
        private readonly Func<FileEntry, object> _loadFunction;

        public RecordingLoader(string extension, Func<FileEntry, object> loadFunction)
        {
            _extension = extension;
            _loadFunction = loadFunction;
            LoadedPaths = new List<UPath>();
        }

        public int LoadCallCount { get; private set; }

        public List<UPath> LoadedPaths { get; }

        public bool CanHandle(string fileExtension)
        {
            return string.Equals(fileExtension, _extension, StringComparison.OrdinalIgnoreCase);
        }

        public object Load(FileEntry file)
        {
            LoadCallCount++;
            LoadedPaths.Add(file.Path);
            return _loadFunction(file);
        }
    }
}
