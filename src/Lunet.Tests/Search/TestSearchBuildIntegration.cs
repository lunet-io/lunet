// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Lunet.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Lunet.Tests.Search;

public class TestSearchBuildIntegration
{
    [Test]
    public async Task TestBuildIndexesMarkdownPagesIntoSqliteDatabase()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/config.scriban",
            """
            baseurl = "https://example.com"

            with search
                enable = true
                engine = "sqlite"
            end
            """);
        context.WriteAllText("/site/readme.md", "# Home\n\nsearchtokenroot");
        context.WriteAllText("/site/docs/readme.md", "# Docs\n\nsearchtokendocs");

        var exitCode = await context.RunAsync("--input-dir=site", "build");

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(context.FileExists("/site/.lunet/build/www/js/lunet-search.sqlite"));

        var sqliteBytes = context.ReadAllBytes("/site/.lunet/build/www/js/lunet-search.sqlite");
        Assert.GreaterOrEqual(GetSqliteRowCount(sqliteBytes), 2);
        Assert.GreaterOrEqual(GetRowsContainingContentToken(sqliteBytes, "searchtokenroot"), 1);
        Assert.GreaterOrEqual(GetRowsContainingContentToken(sqliteBytes, "searchtokendocs"), 1);
    }

    [Test]
    public async Task TestBuildSearchExcludeGlobKeepsNonExcludedPagesIndexed()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/config.scriban",
            """
            baseurl = "https://example.com"

            with search
                enable = true
                engine = "sqlite"
                excludes.add ["/docs/todos/**"]
            end
            """);
        context.WriteAllText("/site/docs/readme.md", "# Docs\n\nsearchvisibletoken");
        context.WriteAllText("/site/docs/todos/private.md", "# Private\n\nsearchhiddentoken");

        var exitCode = await context.RunAsync("--input-dir=site", "build");

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(context.FileExists("/site/.lunet/build/www/js/lunet-search.sqlite"));

        var sqliteBytes = context.ReadAllBytes("/site/.lunet/build/www/js/lunet-search.sqlite");
        Assert.GreaterOrEqual(GetSqliteRowCount(sqliteBytes), 1);
        Assert.GreaterOrEqual(GetRowsContainingContentToken(sqliteBytes, "searchvisibletoken"), 1);
        Assert.AreEqual(0, GetRowsContainingContentToken(sqliteBytes, "searchhiddentoken"));
    }

    [Test]
    public async Task TestBuildOverwritesStaleSearchDatabaseEvenIfOutputIsNewer()
    {
        using var context = new PhysicalLunetAppTestContext();
        context.WriteAllText("site/config.scriban",
            """
            baseurl = "https://example.com"

            with search
                enable = true
                engine = "sqlite"
            end
            """);
        context.WriteAllText("site/readme.md", "# Home\n\nsearchtokenfresh");

        var staleDatabasePath = context.GetAbsolutePath("site/.lunet/build/www/js/lunet-search.sqlite");
        CreateStaleEmptySearchDatabase(staleDatabasePath);
        File.SetLastWriteTimeUtc(staleDatabasePath, DateTime.UtcNow.AddMinutes(5));

        var sitePath = context.GetAbsolutePath("site");
        var exitCode = await context.RunAsync($"--input-dir={sitePath}", "build");

        Assert.AreEqual(0, exitCode);
        Assert.GreaterOrEqual(GetSqliteRowCount(staleDatabasePath), 1);
        Assert.GreaterOrEqual(GetRowsContainingContentToken(staleDatabasePath, "searchtokenfresh"), 1);
    }

    private static int GetSqliteRowCount(byte[] sqliteBytes)
    {
        return ExecuteScalar(sqliteBytes, "SELECT COUNT(*) FROM pages;");
    }

    private static int GetRowsContainingContentToken(byte[] sqliteBytes, string token)
    {
        return ExecuteScalar(sqliteBytes, "SELECT COUNT(*) FROM pages WHERE content LIKE $token;", ("$token", $"%{token}%"));
    }

    private static int GetSqliteRowCount(string sqlitePath)
    {
        return ExecuteScalar(sqlitePath, "SELECT COUNT(*) FROM pages;");
    }

    private static int GetRowsContainingContentToken(string sqlitePath, string token)
    {
        return ExecuteScalar(sqlitePath, "SELECT COUNT(*) FROM pages WHERE content LIKE $token;", ("$token", $"%{token}%"));
    }

    private static int ExecuteScalar(byte[] sqliteBytes, string sql, params (string Name, string Value)[] parameters)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, sqliteBytes);

            using var connection = new SqliteConnection($"Data Source={tempFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }
        finally
        {
            try
            {
                File.Delete(tempFile);
            }
            catch
            {
            }
        }
    }

    private static int ExecuteScalar(string sqlitePath, string sql, params (string Name, string Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={sqlitePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static void CreateStaleEmptySearchDatabase(string sqlitePath)
    {
        var directory = Path.GetDirectoryName(sqlitePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection($"Data Source={sqlitePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DROP TABLE IF EXISTS pages; CREATE TABLE pages(url TEXT, title TEXT, content TEXT);";
        command.ExecuteNonQuery();
        connection.Close();
        SqliteConnection.ClearPool(connection);
    }
}
