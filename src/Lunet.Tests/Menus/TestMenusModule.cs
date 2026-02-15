// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Menus;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Menus;

public class TestMenusModule
{
    [Test]
    public void TestMenuPluginRegistersProcessorAndDefaults()
    {
        using var context = new SiteTestContext();
        var plugin = new MenuPlugin(context.Site);

        Assert.NotNull(plugin.Processor);
        Assert.AreEqual("Home", plugin.HomeTitle);
        Assert.AreSame(plugin, context.Site.GetSafeValue<MenuPlugin>("menu"));
        Assert.AreSame(plugin.Processor, context.Site.Content.AfterRunningProcessors.Find<MenuProcessor>());
        Assert.AreSame(plugin.Processor, context.Site.Content.BeforeProcessingProcessors.Find<MenuProcessor>());
    }

    [Test]
    public void TestMenuProcessorBuildsMenuAndAssignsPageMenuItem()
    {
        using var context = new SiteTestContext();
        var plugin = new MenuPlugin(context.Site);
        var contentPage = context.CreateFileContentObject("/docs/intro.md", "+++\ntitle = \"Intro\"\n+++\nHello", withFrontMatterScript: true);
        contentPage.Initialize();
        context.Site.Pages.Add(contentPage);

        var menuFile = context.CreateFileContentObject(
            "/docs/menu.yml",
            """
            main:
              items:
                - path: intro.md
                  title: Intro
            """);

        var processResult = plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(ContentResult.Break, processResult);
        Assert.IsTrue(menuFile.Discard);

        var menuItem = contentPage.GetSafeValue<MenuObject>("menu_item");
        Assert.NotNull(menuItem);
        Assert.AreEqual("/docs/intro.md", menuItem!.Path);
        Assert.AreEqual("Intro", menuItem.Title);
        Assert.NotNull(menuItem.Parent);
        Assert.AreEqual("main", menuItem.Parent!.Name);
        Assert.AreEqual("Home", menuItem.Parent.Title);
    }

    [Test]
    public void TestMenuProcessorRejectsInvalidMenuPath()
    {
        using var context = new SiteTestContext();
        var plugin = new MenuPlugin(context.Site);
        var menuFile = context.CreateFileContentObject(
            "/docs/menu.yml",
            """
            main:
              items:
                - path:
            """);

        Assert.Throws<LunetException>(() => plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running));
    }

    [Test]
    public void TestMenuObjectPropertiesAndBuiltins()
    {
        var root = new MenuObject
        {
            Name = "main",
            Title = "Main",
            Path = "/index.md",
            Url = "/",
            Folder = true,
            Separator = false
        };

        var child = new MenuObject
        {
            Name = "child",
            Title = "Child",
            Parent = root,
            Path = "/docs/page.md"
        };
        root.Children.Add(child);

        Assert.AreEqual("main", root.Name);
        Assert.AreEqual("/index.md", root.Path);
        Assert.AreEqual("Main", root.Title);
        Assert.AreEqual("/", root.Url);
        Assert.IsTrue(root.Folder);
        Assert.IsFalse(root.Separator);
        Assert.IsTrue(root.HasChildren());
        Assert.AreEqual(1, root.Children.Count);
        Assert.AreSame(root, child.Parent);
        StringAssert.Contains("Menu: Url: / Name: main", root.ToString());
        Assert.IsTrue(root.ContainsKey("render"));
        Assert.IsTrue(root.ContainsKey("breadcrumb"));
    }
}
