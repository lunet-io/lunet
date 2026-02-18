// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Resources;
using Lunet.Search;
using Lunet.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Lunet.Tests.Search;

public class TestSearchModule
{
    [Test]
    public void TestSearchPluginRegistersBuiltinsAndDefaults()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var resourcePlugin = new ResourcePlugin(context.Site);
        var plugin = new SearchPlugin(context.Site, bundlePlugin, resourcePlugin);

        Assert.AreSame(plugin, context.Site.GetSafeValue<SearchPlugin>("search"));
        Assert.IsFalse(plugin.Enable);
        Assert.AreEqual(SqliteSearchEngine.EngineName, plugin.Engine);
        Assert.AreEqual((string)SearchPlugin.DefaultUrl, plugin.Url);
        Assert.NotNull(plugin.Excludes);
        Assert.AreEqual(1, plugin.SearchEngines.Count);
    }

    [Test]
    public void TestSqliteEngineIndexesHtmlContentAndEmitsDatabaseAsset()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var resourcePlugin = new ResourcePlugin(context.Site);
        var plugin = new SearchPlugin(context.Site, bundlePlugin, resourcePlugin)
        {
            Enable = true,
            Engine = SqliteSearchEngine.EngineName
        };

        RunSearchBeforeLoading(context.Site);

        var page = context.CreateFileContentObject("/docs/index.html", "<h1>Title</h1><p>Search body</p>");
        page.Title = "Search Title";
        page.Initialize();
        RunSearchRunning(context.Site, page);

        RunSearchBeforeProcessing(context.Site);
        RunSearchAfterProcessing(context.Site);

        var dbPage = FindDynamicPageByUrl(context.Site.DynamicPages, "/js/lunet-search.sqlite");
        Assert.NotNull(dbPage);
        Assert.AreEqual(1, GetSqliteRowCount(dbPage!));

        var defaultBundle = bundlePlugin.GetOrCreateBundle(null);
        Assert.IsTrue(defaultBundle.Links.Count >= 3);
        Assert.IsTrue(HasBundleLink(defaultBundle, "/modules/search/sqlite/lunet-search-sqlite.js"));
        Assert.IsTrue(HasBundleLink(defaultBundle, "/modules/search/sqlite/lunet-sql-wasm.js"));
        Assert.IsTrue(HasBundleLink(defaultBundle, "/modules/search/sqlite/lunet-sql-wasm.wasm"));
    }

    [Test]
    public void TestSqliteEngineRespectsExcludePaths()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var resourcePlugin = new ResourcePlugin(context.Site);
        var plugin = new SearchPlugin(context.Site, bundlePlugin, resourcePlugin)
        {
            Enable = true,
            Engine = SqliteSearchEngine.EngineName
        };
        plugin.Excludes.Add("/docs/index.html");

        RunSearchBeforeLoading(context.Site);

        var page = context.CreateFileContentObject("/docs/index.html", "<h1>Ignored</h1><p>Should not be indexed</p>");
        page.Initialize();
        RunSearchRunning(context.Site, page);

        RunSearchBeforeProcessing(context.Site);
        RunSearchAfterProcessing(context.Site);

        var dbPage = FindDynamicPageByUrl(context.Site.DynamicPages, "/js/lunet-search.sqlite");
        Assert.NotNull(dbPage);
        Assert.AreEqual(0, GetSqliteRowCount(dbPage!));
    }

    [Test]
    public void TestSqliteEngineIndexesDynamicPagesDuringProcessing()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var resourcePlugin = new ResourcePlugin(context.Site);
        _ = new SearchPlugin(context.Site, bundlePlugin, resourcePlugin)
        {
            Enable = true,
            Engine = SqliteSearchEngine.EngineName
        };

        RunSearchBeforeLoading(context.Site);

        var dynamicPage = new DynamicContentObject(context.Site, "/api/readme.md", "api", "/api/readme.md")
        {
            Title = "API Root",
            ContentType = ContentType.Markdown,
            Content = "# API\n\nsearchdynamicapitoken"
        };
        dynamicPage.Initialize();

        RunSearchProcessing(context.Site, dynamicPage);
        RunSearchBeforeProcessing(context.Site);
        RunSearchAfterProcessing(context.Site);

        var dbPage = FindDynamicPageByUrl(context.Site.DynamicPages, "/js/lunet-search.sqlite");
        Assert.NotNull(dbPage);
        Assert.AreEqual(1, GetSqliteRowCount(dbPage!));
        Assert.AreEqual(1, GetRowsContainingContentToken(dbPage!, "searchdynamicapitoken"));
    }

    [Test]
    public void TestSearchPluginSkipsInvalidEngineName()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var resourcePlugin = new ResourcePlugin(context.Site);
        _ = new SearchPlugin(context.Site, bundlePlugin, resourcePlugin)
        {
            Enable = true,
            Engine = "invalid-engine"
        };

        RunSearchBeforeLoading(context.Site);
        RunSearchBeforeProcessing(context.Site);
        RunSearchAfterProcessing(context.Site);

        Assert.AreEqual(0, context.Site.DynamicPages.Count);
    }

    private static void RunSearchBeforeLoading(SiteObject site)
    {
        foreach (var processor in site.Content.BeforeLoadingProcessors)
        {
            processor.Process(ProcessingStage.BeforeLoadingContent);
        }
    }

    private static void RunSearchRunning(SiteObject site, ContentObject page)
    {
        foreach (var processor in site.Content.AfterRunningProcessors)
        {
            processor.TryProcessContent(page, ContentProcessingStage.Running);
        }
    }

    private static void RunSearchProcessing(SiteObject site, ContentObject page)
    {
        foreach (var processor in site.Content.ContentProcessors)
        {
            processor.TryProcessContent(page, ContentProcessingStage.Processing);
        }
    }

    private static void RunSearchBeforeProcessing(SiteObject site)
    {
        foreach (var processor in site.Content.BeforeProcessingProcessors)
        {
            processor.Process(ProcessingStage.BeforeProcessingContent);
        }
    }

    private static void RunSearchAfterProcessing(SiteObject site)
    {
        foreach (var processor in site.Content.AfterProcessingProcessors)
        {
            processor.Process(ProcessingStage.AfterProcessingContent);
        }
    }

    private static FileContentObject? FindDynamicPageByUrl(PageCollection pages, string expectedUrl)
    {
        foreach (var page in pages)
        {
            if (page is FileContentObject filePage && filePage.Url == expectedUrl)
            {
                return filePage;
            }
        }

        return null;
    }

    private static int GetSqliteRowCount(FileContentObject dbPage)
    {
        var resolvedSourcePath = dbPage.SourceFile.FileSystem!.ResolvePath(dbPage.SourceFile.AbsolutePath);
        var filePath = resolvedSourcePath.FileSystem.ConvertPathToInternal(resolvedSourcePath.Path);

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pages;";
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static int GetRowsContainingContentToken(FileContentObject dbPage, string token)
    {
        var resolvedSourcePath = dbPage.SourceFile.FileSystem!.ResolvePath(dbPage.SourceFile.AbsolutePath);
        var filePath = resolvedSourcePath.FileSystem.ConvertPathToInternal(resolvedSourcePath.Path);

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pages WHERE content LIKE $token;";
        command.Parameters.AddWithValue("$token", $"%{token}%");
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static bool HasBundleLink(BundleObject bundle, string path)
    {
        foreach (var link in bundle.Links)
        {
            if (link.Path == path)
            {
                return true;
            }
        }

        return false;
    }
}
