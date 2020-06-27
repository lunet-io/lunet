using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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
                    _connection.Open();

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = $"CREATE VIRTUAL TABLE pages USING fts5(url, title, content);";
                        command.ExecuteNonQuery();
                    }

                    _currentTransaction = _connection.BeginTransaction();
                }
            }
            else if (stage == ProcessingStage.BeforeProcessingContent)
            {
                if (_connection != null)
                {
                    // Commit all changes generated during loading
                    _currentTransaction?.Commit();
                    _connection?.Close();
                    _connection?.Dispose();

                    // Add our dynamic content to the output
                    var fs = new PhysicalFileSystem();
                    var srcPath = fs.ConvertPathFromInternal(_dbPathOnDisk);
                    var content = new ContentObject(Site, new FileEntry(new PhysicalFileSystem(), srcPath), path: _outputUrl);
                    Site.DynamicPages.Add(content);

                    _currentTransaction = null;
                    _connection = null;
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