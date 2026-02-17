// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Menus;
using Lunet.Tests.Infrastructure;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Tests.Menus;

public class TestMenusModule
{
    private static MenuPlugin CreateMenuPlugin(SiteObject site)
    {
        var bundlePlugin = new BundlePlugin(site);
        return new MenuPlugin(site, bundlePlugin);
    }

    [Test]
    public void TestMenuPluginRegistersProcessorAndDefaults()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);

        Assert.NotNull(plugin.Processor);
        Assert.AreSame(plugin, context.Site.GetSafeValue<MenuPlugin>("menu"));
        Assert.AreSame(plugin.Processor, context.Site.Content.AfterRunningProcessors.Find<MenuProcessor>());
        Assert.AreSame(plugin.Processor, context.Site.Content.BeforeProcessingProcessors.Find<MenuProcessor>());
    }

    [Test]
    public void TestMenuProcessorBuildsMenuAndAssignsPageMenuItem()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);
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
        Assert.AreEqual("Main", menuItem.Parent.Title);
    }

    [Test]
    public void TestMenuProcessorRejectsInvalidMenuPath()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);
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
        Assert.AreEqual(3, root.Width);
        Assert.IsTrue(root.HasChildren());
        Assert.AreEqual(1, root.Children.Count);
        Assert.AreSame(root, child.Parent);
        StringAssert.Contains("Menu: Url: / Name: main", root.ToString());
        Assert.IsTrue(root.ContainsKey("render"));
        Assert.IsTrue(root.ContainsKey("breadcrumb"));

        root.Width = 99;
        Assert.AreEqual(4, root.Width);

        root.Width = 1;
        Assert.AreEqual(2, root.Width);
    }

    [Test]
    public void TestManualFolderMenuAdoptsGeneratedChildren()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);

        var apiRootPage = context.CreateFileContentObject("/api/readme.md", "+++\ntitle = \"API\"\n+++\nAPI", withFrontMatterScript: true);
        apiRootPage.Initialize();
        context.Site.Pages.Add(apiRootPage);

        var namespacePage = context.CreateFileContentObject("/api/demo/readme.md", "+++\ntitle = \"Demo\"\n+++\nDemo", withFrontMatterScript: true);
        namespacePage.Initialize();
        context.Site.Pages.Add(namespacePage);

        var generatedRoot = new MenuObject
        {
            Name = "api",
            Title = "Generated API",
            Path = "/api/readme.md",
            Page = apiRootPage,
            Folder = true,
            Generated = true,
            Width = 4,
        };

        var generatedNamespace = new MenuObject
        {
            Parent = generatedRoot,
            Title = "Demo",
            Path = "/api/demo/readme.md",
            Page = namespacePage,
            Generated = true,
        };
        generatedRoot.Children.Add(generatedNamespace);

        plugin.SetPageMenu(apiRootPage, generatedRoot, force: true);
        plugin.SetPageMenu(namespacePage, generatedNamespace, force: true);

        var menuFile = context.CreateFileContentObject(
            "/menu.yml",
            """
            home:
              items:
                - path: api/readme.md
                  title: API Reference
                  folder: true
            """);

        var processResult = plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(ContentResult.Break, processResult);
        var rootMenuItem = apiRootPage.GetSafeValue<MenuObject>("menu_item");
        Assert.NotNull(rootMenuItem);
        Assert.AreEqual("API Reference", rootMenuItem!.Title);
        Assert.AreEqual(4, rootMenuItem.Width);
        Assert.AreEqual(1, rootMenuItem.Children.Count);
        Assert.AreSame(generatedNamespace, rootMenuItem.Children[0]);
        Assert.AreSame(rootMenuItem, generatedNamespace.Parent);
    }

    [Test]
    public void TestManualNonFolderMenuDoesNotOverrideGeneratedMenu()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);

        var apiRootPage = context.CreateFileContentObject("/api/readme.md", "+++\ntitle = \"API\"\n+++\nAPI", withFrontMatterScript: true);
        apiRootPage.Initialize();
        context.Site.Pages.Add(apiRootPage);

        var generatedRoot = new MenuObject
        {
            Name = "api",
            Title = "Generated API",
            Path = "/api/readme.md",
            Page = apiRootPage,
            Folder = true,
            Generated = true,
        };
        plugin.SetPageMenu(apiRootPage, generatedRoot, force: true);

        var menuFile = context.CreateFileContentObject(
            "/menu.yml",
            """
            home:
              items:
                - path: api/readme.md
                  title: API
            """);

        plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        var rootMenuItem = apiRootPage.GetSafeValue<MenuObject>("menu_item");
        Assert.NotNull(rootMenuItem);
        Assert.AreEqual("Generated API", rootMenuItem!.Title);
    }

    [Test]
    public void TestRenderKeepsGeneratedSubmenusExpandedForCurrentPath()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);

        var apiRootPage = context.CreateFileContentObject("/api/readme.md", "+++\ntitle = \"API\"\n+++\nAPI", withFrontMatterScript: true);
        apiRootPage.Initialize();
        context.Site.Pages.Add(apiRootPage);

        var namespacePage = context.CreateFileContentObject("/api/demo/readme.md", "+++\ntitle = \"Demo\"\n+++\nDemo", withFrontMatterScript: true);
        namespacePage.Initialize();
        context.Site.Pages.Add(namespacePage);

        var methodPage = context.CreateFileContentObject("/api/demo/run/readme.md", "+++\ntitle = \"Run\"\n+++\nRun", withFrontMatterScript: true);
        methodPage.Initialize();
        context.Site.Pages.Add(methodPage);

        var rootMenu = new MenuObject
        {
            Name = "api",
            Title = "API Reference",
            Path = "/api/readme.md",
            Page = apiRootPage,
            Folder = true,
            Generated = true,
        };

        var namespaceMenu = new MenuObject
        {
            Parent = rootMenu,
            Title = "Demo",
            Path = "/api/demo/readme.md",
            Page = namespacePage,
            Folder = true,
            Generated = true,
        };
        rootMenu.Children.Add(namespaceMenu);

        var methodsGroup = new MenuObject
        {
            Parent = namespaceMenu,
            Title = "Methods",
            Folder = true,
            Generated = true,
        };
        namespaceMenu.Children.Add(methodsGroup);

        var methodMenu = new MenuObject
        {
            Parent = methodsGroup,
            Title = "Run()",
            Path = "/api/demo/run/readme.md",
            Page = methodPage,
            Generated = true,
        };
        methodsGroup.Children.Add(methodMenu);

        plugin.SetPageMenu(methodPage, methodMenu, force: true);

        var scriptContext = new TemplateContext();
        var globals = new ScriptObject();
        globals.SetValue("page", methodPage, true);
        scriptContext.PushGlobal(globals);

        var html = rootMenu.Render(scriptContext, default, new ScriptObject
        {
            ["kind"] = "menu",
            ["collapsible"] = true,
            ["depth"] = 6,
        });

        StringAssert.Contains("Methods", html);
        StringAssert.Contains("Run()", html);
        StringAssert.Contains("aria-expanded='true'", html);
        StringAssert.DoesNotContain("menu-link-show collapsed", html);
        StringAssert.Contains("menu menu-level1 collapse show", html);
        StringAssert.Contains("menu menu-level2 collapse show", html);
    }

    [Test]
    public void TestMenuRootSelfEntryAliasesBreadcrumbRootAndAvoidsDuplicateOnSelfPage()
    {
        using var context = new SiteTestContext();
        var plugin = CreateMenuPlugin(context.Site);

        var homePage = context.CreateFileContentObject("/readme.md", "+++\ntitle = \"Home\"\n+++\nHome", withFrontMatterScript: true);
        homePage.Initialize();
        context.Site.Pages.Add(homePage);

        var introPage = context.CreateFileContentObject("/docs/intro.md", "+++\ntitle = \"Intro\"\n+++\nIntro", withFrontMatterScript: true);
        introPage.Initialize();
        context.Site.Pages.Add(introPage);

        var menuFile = context.CreateFileContentObject(
            "/menu.yml",
            """
            home:
              - {path: readme.md, title: "<i class='bi bi-house-door' aria-hidden='true'></i> Home", self: true}
              - {path: docs/intro.md, title: "Intro"}
            """);

        plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        var root = plugin.GetSafeValue<MenuObject>("home");
        Assert.NotNull(root);
        Assert.AreSame(homePage, root!.Page);
        Assert.NotNull(root.Title);
        StringAssert.Contains("bi-house-door", root.Title!);

        // Ensure breadcrumb on the self page doesn't show the root twice.
        var templateContext = new TemplateContext();
        var globals = new ScriptObject();
        globals.SetValue("page", homePage, true);
        templateContext.PushGlobal(globals);

        var breadcrumb = root.RenderBreadcrumb(templateContext, default(SourceSpan), null);
        Assert.AreEqual(1, Regex.Matches(breadcrumb, "bi-house-door").Count);
    }
}
