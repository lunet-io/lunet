// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Lunet.Core;
using NUglify;
using NUglify.Html;
using Scriban.Functions;

namespace Lunet.Summarizer
{
    public static class SummarizerHelper
    {
        public const int DefaultSummaryWordCount = 70;
        
        public static void UpdateSummary(ContentObject page)
        {
            // Use specific settings to extract text from html
            var settings = new HtmlSettings()
            {
                RemoveComments = false,
                RemoveOptionalTags = false,
                RemoveEmptyAttributes = false,
                RemoveQuotedAttributes = false,
                DecodeEntityCharacters = true,
                RemoveScriptStyleTypeAttribute = false,
                ShortBooleanAttribute = false,
                MinifyJs = false,
                MinifyCss = false,
                MinifyCssAttributes = false

            };

            var parser = new HtmlParser($"<body>{page.Content}</body>", (string)page.SourceFile.Path, settings);
            var document = parser.Parse();

            //var errors = new List<UglifyError>(parser.Errors);

            if (document != null)
            {
                var minifier = new HtmlMinifier(document, settings);
                minifier.Minify();

                var keepFormatting = page.GetSafeValueFromPageOrSite<bool>(PageVariables.SummaryKeepFormatting);
                var maxSummaryCountWord = page.GetSafeValueFromPageOrSite<int>(PageVariables.SummaryWordCount, DefaultSummaryWordCount);
                if (maxSummaryCountWord < 0) maxSummaryCountWord = DefaultSummaryWordCount;

                // We detect if the document has already a <!--more--> attribute, if yes, we will use it has the summary
                bool hasMore = false;
                try
                {
                    var checkHasMore = new HtmlVisitHasMore();
                    checkHasMore.Write(document);
                }
                catch (HtmlWriterEarlyExitException)
                {
                    hasMore = true;
                }

                
                // Write the content with early exit
                var writer = new StringWriter();
                try
                {
                    var htmlWriter = new HtmlWriterToSummary(writer, keepFormatting ? HtmlToTextOptions.KeepFormatting : HtmlToTextOptions.KeepHtmlEscape)
                    {
                        HasMore = hasMore, 
                        MaxSummaryWordCount = maxSummaryCountWord
                    };
                    htmlWriter.Write(document);
                }
                catch (HtmlWriterEarlyExitException)
                {
                    // ignore
                }

                var fullText = writer.ToString();
                var summary = hasMore ? fullText : StringFunctions.Truncatewords(fullText, maxSummaryCountWord);

                page.Summary = summary;
            }

            //foreach (var message in errors)
            //{
            //    if (message.IsError)
            //    {
            //        page.Site.Error(message.ToString());
            //    }
            //    else
            //    {
            //        page.Site.Warning(message.ToString());
            //    }
            //}
        }

        /// <summary>
        /// Html writer that early exists if the number of word count has been reached or if we have found a more comment.
        /// </summary>
        private class HtmlWriterToSummary : HtmlWriterToText
        {
            private static readonly Regex CountWordsRegex = new Regex(@"\w+");
            private int _totalRunningNumberOfWords;
            
            public HtmlWriterToSummary(TextWriter writer, HtmlToTextOptions options) : base(writer, options)
            {
            }
            
            public int MaxSummaryWordCount { get; set; }
            
            public bool HasMore { get; set; }

            protected override void Write(HtmlComment node)
            {
                var sliceText = node.Slice.ToString().Trim();
                if (sliceText == "more") throw new HtmlWriterEarlyExitException();
            }

            protected override void Write(string text)
            {
                base.Write(text);
                
                // If we are looking more <!-- more --> then don't limit by word count
                if (!HasMore)
                {
                    _totalRunningNumberOfWords += CountWordsRegex.Matches(text).Count;
                    if (_totalRunningNumberOfWords >= MaxSummaryWordCount) throw new HtmlWriterEarlyExitException();
                }
            }
        }

        private class HtmlVisitHasMore : HtmlWriterBase
        {
            protected override void Write(HtmlComment node)
            {
                var sliceText = node.Slice.ToString().Trim();
                if (sliceText == "more") throw new HtmlWriterEarlyExitException();
            }

            protected override void Write(string text)
            {
            }

            protected override void Write(char c)
            {
            }
        }

        /// <summary>
        /// Exception used for early exit in <see cref="HtmlVisitHasMore"/> and <see cref="HtmlWriterToSummary"/>.
        /// </summary>
        private class HtmlWriterEarlyExitException : Exception
        {
        }
    }
}