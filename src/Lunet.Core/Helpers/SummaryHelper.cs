// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using NUglify;
using NUglify.Html;
using Scriban.Functions;
using Scriban.Helpers;
using Scriban.Runtime;

namespace Lunet.Helpers
{
    public static class SummaryHelper
    {
        public static void UpdateSummary(ContentObject page)
        {
            if (page.Content == null || page.ContentType != ContentType.Html)
            {
                return;
            }
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

            var parser = new HtmlParser(page.Content, (string)page.SourceFile.Path, settings);
            var document = parser.Parse();

            var errors = new List<UglifyError>(parser.Errors);

            if (document != null)
            {
                var minifier = new HtmlMinifier(document, settings);
                minifier.Minify();

                var sumarrySettings = (DynamicObject) page;
                if (!sumarrySettings.Contains(PageVariables.SummaryKeepFormatting))
                {
                    sumarrySettings = page.Site;
                }

                var keepFormatting = sumarrySettings.GetSafeValue<bool>(PageVariables.SummaryKeepFormatting);

                errors.AddRange(minifier.Errors);

                var writer = new StringWriter();
                var htmlWriter = new HtmlWriterToSummary(writer, keepFormatting ? HtmlToTextOptions.KeepFormatting : HtmlToTextOptions.KeepHtmlEscape);
                htmlWriter.Write(document);

                var fullText = writer.ToString();

                var readMoreIndex = fullText.IndexOf("<!--more-->", StringComparison.Ordinal);
                
                var summary = readMoreIndex >= 0 ? fullText.Substring(0, readMoreIndex) : StringFunctions.Truncatewords(fullText, 70);

                page.Summary = summary;
            }

            foreach (var message in errors)
            {
                if (message.IsError)
                {
                    page.Site.Error(message.ToString());
                }
                else
                {
                    page.Site.Warning(message.ToString());
                }
            }
        }

        private class HtmlWriterToSummary : HtmlWriterToText
        {
            public HtmlWriterToSummary(TextWriter writer, HtmlToTextOptions options) : base(writer, options)
            {
            }

            protected override void Write(HtmlComment node)
            {
                // Keep comment
                Write("<!--");
                var comment = node.Slice.ToString();
                Write(comment);
                Write("-->");
            }
        }
    }
}