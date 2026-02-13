// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using Lunet.Bundles;
using Lunet.Core;
using Microsoft.Data.Sqlite;
using Zio;
using Zio.FileSystems;

namespace Lunet.Search;

/// <summary>
/// Search based on sqlite.
/// </summary>
public class SqliteSearchEngine : SearchEngine
{
    private SqliteConnection _connection;
    private string _dbPathOnDisk;
    private SqliteTransaction _currentTransaction;
    private readonly object _connectionLock = new();

    public const string EngineName = "sqlite";

    public SqliteSearchEngine(SearchPlugin plugin) : base(plugin, EngineName)
    {
    }

    public override void Initialize()
    {
        _dbPathOnDisk = Path.GetTempFileName();
        _connection = new SqliteConnection($"Data Source={_dbPathOnDisk}");

        try
        {
            _connection.Open();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA page_size = 512; PRAGMA JOURNAL_MODE = memory;";
                command.ExecuteNonQuery();

                command.CommandText = $"CREATE VIRTUAL TABLE pages USING fts5(url, title, content, tokenize = porter);";
                command.ExecuteNonQuery();
            }

            _currentTransaction = _connection.BeginTransaction();
        }
        catch
        {
            try
            {
                _connection.Dispose();
            }
            finally
            {
                _connection = null;
                _currentTransaction = null;
            }

            // re-throw
            throw;
        }
    }

    public override void ProcessSearchContent(ContentObject file, string plainText)
    {
        lock (_connectionLock)
        {
            // Create the insert command that will be used by the content processing stage
            using (var insertContentCommand = _connection.CreateCommand())
            {
                var urlParameter = insertContentCommand.CreateParameter();
                urlParameter.ParameterName = "$url";
                var titleParameter = insertContentCommand.CreateParameter();
                titleParameter.ParameterName = "$title";
                var contentParameter = insertContentCommand.CreateParameter();
                contentParameter.ParameterName = "$content";

                insertContentCommand.CommandText = $"INSERT INTO pages(url, title, content) VALUES({urlParameter.ParameterName}, {titleParameter.ParameterName}, {contentParameter.ParameterName});";

                insertContentCommand.Parameters.Add(urlParameter);
                insertContentCommand.Parameters.Add(titleParameter);
                insertContentCommand.Parameters.Add(contentParameter);

                urlParameter.Value = file.Url ?? string.Empty;
                titleParameter.Value = file.Title ?? string.Empty;
                contentParameter.Value = plainText ?? string.Empty;

                insertContentCommand.ExecuteNonQuery();
            }
        }
    }

    public override void Terminate()
    {
        if (_connection == null) return;

        // Last pass by optimizing the b-tree
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO pages(pages) VALUES('optimize');";
            command.ExecuteNonQuery();
        }

        // Commit all changes generated during loading
        _currentTransaction.Commit();

        // Final compaction of the DB
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "VACUUM;";
            command.ExecuteNonQuery();
        }

        _connection.Close();
        _connection.Dispose();

        // Now required https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-6.0/breaking-changes#connection-pool
        // otherwise the local file will be locked and we won't be able to copy it
        SqliteConnection.ClearPool(_connection);

        // Add our dynamic content to the output
        var fs = new PhysicalFileSystem();
        var srcPath = fs.ConvertPathFromInternal(_dbPathOnDisk);
        var content = new FileContentObject(Site, new FileSystemItem(fs, srcPath, false), path: OutputUrl.ChangeExtension("sqlite"));
        content.Initialize();
        Site.DynamicPages.Add(content);

        _currentTransaction = null;
        _connection = null;

        // TODO: make it configurable by selecting which bundle will receive the search/db
        var defaultBundle = Plugin.BundlePlugin.GetOrCreateBundle(null);

        if (Plugin.Worker)
        {
            defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/sqlite/lunet-sql-wasm.wasm", "/js/lunet-sql-wasm.wasm");
            defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/sqlite/lunet-search-sqlite.js", "/js/lunet-search.js");
            defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/sqlite/lunet-sql-wasm.js", "/js/lunet-sql-wasm.js");
            defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-search-ws-client.js");
        }
        else
        {
            // Insert content before the others to make sure they are loaded async ASAP
            defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/sqlite/lunet-sql-wasm.wasm", "/js/lunet-sql-wasm.wasm");
            defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-search-sqlite.js");
            defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-sql-wasm.js");
        }
    }
}