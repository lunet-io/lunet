using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Lunet.Core;
using Lunet.Helpers;
using Markdig;
using Zio;

namespace Lunet.Search
{
    public abstract class SearchEngine : ContentProcessor<SearchPlugin>
    {
        private readonly List<SearchPattern> _excludes;
        private UPath _outputUrl;
        private bool _isInitialized;

        protected SearchEngine(SearchPlugin plugin, string name) : base(plugin)
        {
            _excludes = new List<SearchPattern>();
            Name = name;
        }

        public override string Name { get; }

        protected UPath OutputUrl => _outputUrl;

        public abstract void Initialize();

        public abstract void ProcessSearchContent(ContentObject file, string plainText);

        public abstract void Terminate();

        public override void Process(ProcessingStage stage)
        {
            if (stage == ProcessingStage.BeforeLoadingContent)
            {
                if (!Plugin.Enable) return;

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

                try
                {
                    Initialize();
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    Site.Error($"Unable to initialize search processor `{Name}`. Reason: {ex.Message}");
                }
            }
            else if (stage == ProcessingStage.BeforeProcessingContent)
            {
               Terminate();
            }
        }

        private static readonly Regex FindSpacesRegexp = new Regex(@"\s+");

        public override ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage)
        {
            Debug.Assert(stage == ContentProcessingStage.Running);

            if (!_isInitialized) return ContentResult.Continue;

            var contentType = file.ContentType;

            // This plugin is only working on scss files
            if (!contentType.IsHtmlLike())
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

            ProcessSearchContent(file, plainText);

            return ContentResult.Continue;
        }
    }
}