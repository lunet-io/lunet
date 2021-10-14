// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lunet.Markdown.Extensions
{
    public class XRefMarkdownExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.InlineParsers.InsertBefore<AutolinkInlineParser>(new XRefInlineParser());
            
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private class XRefInlineParser : InlineParser
        {
            public XRefInlineParser()
            {
                OpeningCharacters = new[] {'<'};
            }

            public override bool Match(InlineProcessor processor, ref StringSlice slice)
            {
                var start = slice;
                var end = slice;
                int line;
                int column;
                
                end.NextChar(); // skip <
                
                var startLink = end.Start;
                if (!end.Match("xref:"))
                {
                    return false;
                }
                end.Start += "xref:".Length;
                var c = end.CurrentChar;
                var endLinkXref = end.IndexOf('>');
                if (endLinkXref < 0)
                {
                    return false;
                }
                end.Start = endLinkXref + 1;
                slice = end;

                var endLink = endLinkXref - 1;
                var link = slice.Text.Substring(startLink, endLink - startLink + 1);
                
                processor.Inline = new AutolinkInline(link)
                {
                    Span = new SourceSpan(processor.GetSourcePosition(start.Start, out line, out column), processor.GetSourcePosition(endLinkXref)),
                    Line = line,
                    Column = column
                };

                return true;
            }
        }
    }
}