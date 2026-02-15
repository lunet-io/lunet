// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Tests.Infrastructure;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Core;

public class TestCoreFileSystemsAndConfiguration
{
    [Test]
    public void TestSiteConfigurationRegistersPluginsWithoutDuplicates()
    {
        using var context = new SiteTestContext();

        context.Configuration.RegisterPlugin<TestPlugin>();
        context.Configuration.RegisterPlugin<TestPlugin>();

        var pluginTypes = context.Configuration.PluginTypes();
        Assert.AreEqual(1, pluginTypes.Count);
        Assert.AreEqual(typeof(TestPlugin), pluginTypes[0]);
    }

    [Test]
    public void TestSiteConfigurationRegisterPluginRejectsInvalidType()
    {
        using var context = new SiteTestContext();

        var ex = Assert.Throws<ArgumentException>(() => context.Configuration.RegisterPlugin(typeof(string)));
        Assert.IsNotNull(ex);
        StringAssert.Contains("ISitePlugin", ex!.Message);
    }

    [Test]
    public void TestInMemorySiteFileSystemsDefaultInitialization()
    {
        var fileSystems = new InMemorySiteFileSystems();
        fileSystems.Initialize();

        Assert.NotNull(fileSystems.InputFileSystem);

        new FileEntry(fileSystems.InputFileSystem!, "/input.txt").WriteAllText("input");
        new FileEntry(fileSystems.OutputFileSystem, "/output.txt").WriteAllText("output");

        Assert.IsTrue(fileSystems.WorkspaceFileSystem.FileExists("/input.txt"));
        Assert.IsTrue(fileSystems.WorkspaceFileSystem.FileExists("/.lunet/build/www/output.txt"));
    }

    [Test]
    public void TestInMemorySiteFileSystemsCustomInitialization()
    {
        var fileSystems = new InMemorySiteFileSystems();
        fileSystems.Initialize("docs/site", "public/www");

        new FileEntry(fileSystems.InputFileSystem!, "/input.txt").WriteAllText("input");
        new FileEntry(fileSystems.OutputFileSystem, "/output.txt").WriteAllText("output");

        Assert.IsTrue(fileSystems.WorkspaceFileSystem.FileExists("/docs/site/input.txt"));
        Assert.IsTrue(fileSystems.WorkspaceFileSystem.FileExists("/public/www/output.txt"));
    }

    [Test]
    public void TestSiteFileSystemsInputFileSystemTakesPriorityOverContentFileSystems()
    {
        var fileSystems = new InMemorySiteFileSystems();
        fileSystems.Initialize();
        new FileEntry(fileSystems.InputFileSystem!, "/shared.txt").WriteAllText("input");

        var contentFileSystem = new MemoryFileSystem();
        new FileEntry(contentFileSystem, "/shared.txt").WriteAllText("content");
        fileSystems.AddContentFileSystem(contentFileSystem);

        Assert.AreEqual("input", new FileEntry(fileSystems.FileSystem, "/shared.txt").ReadAllText());

        fileSystems.ClearContentFileSystems();
        Assert.AreEqual("input", new FileEntry(fileSystems.FileSystem, "/shared.txt").ReadAllText());
    }

    [Test]
    public void TestSiteFileSystemsAddContentFileSystemRejectsNull()
    {
        var fileSystems = new InMemorySiteFileSystems();
        fileSystems.Initialize();

        Assert.Throws<ArgumentNullException>(() => fileSystems.AddContentFileSystem(null!));
    }

    [Test]
    public void TestContentTypeManagerBuiltinsAndFallback()
    {
        var manager = new ContentTypeManager();

        Assert.AreEqual(ContentType.Markdown, manager.GetContentType(".md"));
        Assert.AreEqual(ContentType.Html, manager.GetContentType(".sbn-html"));
        Assert.AreEqual(ContentType.Html, manager.GetContentType("html"));
        Assert.AreEqual(new ContentType("unknown"), manager.GetContentType(".unknown"));
    }

    [Test]
    public void TestContentTypeManagerHtmlRegistrationAndExtensionLookup()
    {
        var manager = new ContentTypeManager();
        var customType = new ContentType("custom");

        manager.AddContentType(".abc", customType);
        manager.RegisterHtmlContentType(customType);

        Assert.AreEqual(customType, manager.GetContentType(".abc"));
        Assert.IsTrue(manager.IsHtmlContentType(customType));
        CollectionAssert.Contains(manager.GetExtensionsByContentType(customType), ".abc");
        CollectionAssert.Contains(manager.GetExtensionsByContentType(new ContentType("xyz")), ".xyz");
    }

    [Test]
    public void TestContentTypeHtmlLikeAndEquality()
    {
        Assert.IsTrue(ContentType.Html.IsHtmlLike());
        Assert.IsTrue(ContentType.Markdown.IsHtmlLike());
        Assert.IsFalse(ContentType.Css.IsHtmlLike());
        Assert.IsTrue(new ContentType("a") == new ContentType("a"));
        Assert.IsTrue(new ContentType("a") != new ContentType("b"));
    }

    private sealed class TestPlugin : SitePlugin
    {
        public TestPlugin(SiteObject site) : base(site)
        {
        }
    }
}
