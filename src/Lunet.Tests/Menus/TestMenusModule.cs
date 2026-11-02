// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public void TestMenuEntryEnvFiltersByCurrentEnvironment()
    {
        using var context = new SiteTestContext();
        context.Site.Environment = "prod";
        var plugin = CreateMenuPlugin(context.Site);

        var homePage = context.CreateFileContentObject("/readme.md", "+++\ntitle = \"Home\"\n+++\nHome", withFrontMatterScript: true);
        homePage.Initialize();
        context.Site.Pages.Add(homePage);

        var devPage = context.CreateFileContentObject("/docs/dev.md", "+++\ntitle = \"Dev\"\n+++\nDev", withFrontMatterScript: true);
        devPage.Initialize();
        context.Site.Pages.Add(devPage);

        var prodPage = context.CreateFileContentObject("/docs/prod.md", "+++\ntitle = \"Prod\"\n+++\nProd", withFrontMatterScript: true);
        prodPage.Initialize();
        context.Site.Pages.Add(prodPage);

        var menuFile = context.CreateFileContentObject(
            "/menu.yml",
            """
            home:
              items:
                - { path: readme.md, title: Home }
                - { path: docs/dev.md, title: DevOnly, env: dev }
                - { path: docs/prod.md, title: ProdOnly, env: prod }
            """);

        plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        var rootMenu = plugin.GetSafeValue<MenuObject>("home");
        Assert.NotNull(rootMenu);
        Assert.AreEqual(2, rootMenu!.Children.Count);
        Assert.AreEqual("Home", rootMenu.Children[0].Title);
        Assert.AreEqual("ProdOnly", rootMenu.Children[1].Title);
        Assert.IsNull(devPage.GetSafeValue<MenuObject>("menu_item"));
        Assert.NotNull(prodPage.GetSafeValue<MenuObject>("menu_item"));
    }

    [Test]
    public void TestMenuEntryEnvSupportsArrayValues()
    {
        using var context = new SiteTestContext();
        context.Site.Environment = "dev";
        var plugin = CreateMenuPlugin(context.Site);

        var homePage = context.CreateFileContentObject("/readme.md", "+++\ntitle = \"Home\"\n+++\nHome", withFrontMatterScript: true);
        homePage.Initialize();
        context.Site.Pages.Add(homePage);

        var previewPage = context.CreateFileContentObject("/docs/preview.md", "+++\ntitle = \"Preview\"\n+++\nPreview", withFrontMatterScript: true);
        previewPage.Initialize();
        context.Site.Pages.Add(previewPage);

        var menuFile = context.CreateFileContentObject(
            "/menu.yml",
            """
            home:
              items:
                - { path: readme.md, title: Home }
                - path: docs/preview.md
                  title: Preview
                  env: [dev, preview]
            """);

        plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        var rootMenu = plugin.GetSafeValue<MenuObject>("home");
        Assert.NotNull(rootMenu);
        Assert.AreEqual(2, rootMenu!.Children.Count);
        Assert.AreEqual("Preview", rootMenu.Children[1].Title);
        Assert.NotNull(previewPage.GetSafeValue<MenuObject>("menu_item"));
    }

    [Test]
    public void TestMenuEntryEnvWorksWithInlineArraySyntax()
    {
        using var context = new SiteTestContext();
        context.Site.Environment = "prod";
        var plugin = CreateMenuPlugin(context.Site);

        var homePage = context.CreateFileContentObject("/readme.md", "+++\ntitle = \"Home\"\n+++\nHome", withFrontMatterScript: true);
        homePage.Initialize();
        context.Site.Pages.Add(homePage);

        var menuFile = context.CreateFileContentObject(
            "/menu.yml",
            """
            home:
              - {path: readme.md, title: "<i class='bi bi-house-door' aria-hidden='true'></i> Home", self: true}
              - {path: api/readme.md, title: "<i class='bi bi-braces' aria-hidden='true'></i> API", folder: true, env: dev}
            """);

        plugin.Processor.TryProcessContent(menuFile, ContentProcessingStage.Running);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);
        plugin.Processor.Process(ProcessingStage.BeforeProcessingContent);

        var rootMenu = plugin.GetSafeValue<MenuObject>("home");
        Assert.NotNull(rootMenu);
        Assert.AreEqual(1, rootMenu!.Children.Count);
        Assert.NotNull(rootMenu.Children[0].Title);
        StringAssert.Contains("Home", rootMenu.Children[0].Title!);
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

    [Test]
    public async Task TestFolderMenuHighlightsOverviewItemOnFolderPage()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/config.scriban",
            """
            baseurl = "https://example.com"
            """);

        context.WriteAllText("/site/docs/menu.yml",
            """
            doc:
              - {path: readme.md, title: "Docs home"}
              - {path: guide/readme.md, title: "Guide", folder: true}
              - {path: p01.md, title: "Page 01"}
            """);

        // The first item of the folder menu is the folder page itself (an overview page)
        context.WriteAllText("/site/docs/guide/menu.yml",
            """
            guide:
              - {path: readme.md, title: "Guide overview"}
              - {path: g1.md, title: "Guide 1"}
              - {path: g2.md, title: "Guide 2"}
            """);

        context.WriteAllText("/site/.lunet/layouts/default.sbn-html",
            """
            ---
            layout: _default
            ---
            <nav class='page-menu'>{{ page.menu.render { kind: "menu", collapsible: true, depth: 6, async: false } }}</nav>
            <nav class='page-menu'>{{ page.menu.render { kind: "menu", collapsible: true, depth: 1, async: false } }}</nav>
            <nav class='page-menu'>{{ site.menu.doc.render { kind: "menu", collapsible: true, depth: 6, async: false } }}</nav>
            """);

        foreach (var name in new[] { "readme", "p01", "guide/readme", "guide/g1", "guide/g2" })
        {
            context.WriteAllText($"/site/docs/{name}.md",
                $"""
                ---
                layout: default
                ---
                # {name}
                """);
        }

        var exitCode = await context.RunAsync("--input-dir=site", "build", "--dev");
        Assert.AreEqual(0, exitCode);

        // The folder menu of the folder page highlights its overview item
        AssertActiveMenuItems(context, "/docs/guide/", 0, ["/docs/guide/"]);
        AssertActiveMenuItems(context, "/docs/guide/", 1, ["/docs/guide/"]);
        // and the doc menu its folder item
        AssertActiveMenuItems(context, "/docs/guide/", 2, ["/docs/guide/"]);

        // The other pages of the folder highlight only their own item
        AssertActiveMenuItems(context, "/docs/guide/g1/", 0, ["/docs/guide/g1/"]);
        AssertActiveMenuItems(context, "/docs/guide/g2/", 1, ["/docs/guide/g2/"]);
        AssertActiveMenuItems(context, "/docs/p01/", 0, ["/docs/p01/"]);
    }

    private static void AssertActiveMenuItems(LunetAppTestContext context, string pageUrl, int menuIndex, string[] expectedActiveUrls)
    {
        var pageHtml = context.ReadAllText($"/site/.lunet/build/www{pageUrl}index.html");
        var menus = Regex.Matches(pageHtml, "<nav class='page-menu'>(.*?)</nav>", RegexOptions.Singleline);
        Assert.Greater(menus.Count, menuIndex, $"Missing menu #{menuIndex} in page {pageUrl}");

        var activeUrls = Regex.Matches(menus[menuIndex].Groups[1].Value, "<li [^>]*class='[^']*\\bactive\\b[^']*'[^>]*>\\s*<span[^>]*><a href='([^']*)'")
            .Select(x => x.Groups[1].Value);
        Assert.AreEqual(string.Join(", ", expectedActiveUrls), string.Join(", ", activeUrls), $"Active items of menu #{menuIndex} in page {pageUrl}");
    }
}
