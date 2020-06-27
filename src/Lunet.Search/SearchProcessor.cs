using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Helpers;
using Markdig;
using Microsoft.Data.Sqlite;
using Zio;
using Zio.FileSystems;

namespace Lunet.Search
{
    public class SearchProcessor : ContentProcessor<SearchPlugin>
    {
        private SqliteConnection _connection;

        private string _dbPathOnDisk;
        private SqliteTransaction _currentTransaction;
        private readonly List<SearchPattern> _excludes;
        private UPath _outputUrl;
        
        public SearchProcessor(SearchPlugin plugin) : base(plugin)
        {
            _excludes = new List<SearchPattern>();
        }

        public override void Process(ProcessingStage stage)
        {
            if (stage == ProcessingStage.BeforeLoadingContent)
            {
                if (Plugin.Enable)
                {
                    if (Plugin.Url == null || !UPath.TryParse(Plugin.Url, out _outputUrl) || !_outputUrl.IsAbsolute)
                    {
                        Site.Error($"Invalid url `{Plugin.Url}` declared for search. Search will not be generated.");
                        return;
                    }

                    _excludes.Clear();

                    // Exclude any files that are globally excluded
                    foreach (var excludeItem in Plugin.Excludes)
                    {
                        if (excludeItem is string str && UPath.TryParse(str, out var excludePath) && excludePath.IsAbsolute)
                        {
                            var searchPattern = excludePath.SearchPattern();
                            _excludes.Add(searchPattern);
                        }
                    }

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
            }
            else if (stage == ProcessingStage.BeforeProcessingContent)
            {
                if (_connection != null)
                {
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

                    // Add our dynamic content to the output
                    var fs = new PhysicalFileSystem();
                    var srcPath = fs.ConvertPathFromInternal(_dbPathOnDisk);
                    var content = new ContentObject(Site, new FileEntry(new PhysicalFileSystem(), srcPath), path: _outputUrl);
                    Site.DynamicPages.Add(content);

                    _currentTransaction = null;
                    _connection = null;

                    // TODO: make it configurable by selecting which bundle will receive the search/db
                    var defaultBundle = Plugin.BundlePlugin.GetOrCreateBundle(null);

                    // Insert content before the others to make sure they are loaded async ASAP
                    defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/lunet-sql-wasm.wasm", "/js/lunet-sql-wasm.wasm");
                    defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/lunet-search.js");
                    defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/lunet-sql-wasm.js");
                }
            }
        }

        private static readonly Regex FindSpacesRegexp = new Regex(@"\s+");

        public override ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage)
        {
            Debug.Assert(stage == ContentProcessingStage.AfterLoading);

            if (_connection == null) return ContentResult.Continue;

            var contentType = file.ContentType;

            // This plugin is only working on scss files
            if (!(contentType == ContentType.Markdown || contentType == ContentType.Html))
            {
                return ContentResult.Continue;
            }

            // Exclude any files that are globally excluded
            foreach (var searchPattern in _excludes)
            {
                if (searchPattern.Match(file.Path))
                {
                    return ContentResult.Continue;
                }
            }

            if (file.Content == null)
            {
                file.Content = file.SourceFile.ReadAllText();
            }

            var plainText = contentType == ContentType.Markdown ? Markdown.ToHtml(file.Content) : file.Content;

            // Remove any HTML from existing content
            plainText = contentType == ContentType.Markdown ? NUglify.Uglify.HtmlToText($"<html><body>{plainText}</body></html>").Code : NUglify.Uglify.HtmlToText(plainText).Code;
            // Remove any trailing 
            plainText = FindSpacesRegexp.Replace(plainText, " ").Trim();

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

            return ContentResult.Continue;
        }
    }
}