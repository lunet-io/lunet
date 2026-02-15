// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Lunet.Api.DotNet.Extractor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DocAsCode.Metadata.ManagedReference;
using NUnit.Framework;

namespace Lunet.Tests.Api.DotNet;

public class TestExtraDocParser
{
    [Test]
    public void TestParserSingle()
    {
        var (items, diagnostics) = ParseExtraDoc("path.md", @"---
uid: Test
---

# Summary
yes summary
# Remarks
yes remarks
# Example
yes example 1
# Example
yes example 2
");
        Assert.NotNull(items);
        Assert.IsEmpty(diagnostics);
        Assert.AreEqual(1, items.Count, "Expecting one item parsed");

        var item = items[0];

        Assert.AreEqual("Test", item.Name);
        Assert.IsTrue(item.IsExtraDoc);
        Assert.AreEqual("path.md", item.ExtraDocFilePath);
        Assert.AreEqual("yes summary", item.Summary?.Trim());
        Assert.AreEqual("yes remarks", item.Remarks?.Trim());

        Assert.NotNull(item.Examples);
        Assert.AreEqual(2, item.Examples.Count, "Expecting 2 examples parsed");
        Assert.AreEqual("yes example 1", item.Examples[0]?.Trim());
        Assert.AreEqual("yes example 2", item.Examples[1]?.Trim());
    }


    [Test]
    public void TestParserMultiple()
    {
        int itemCount = 5;
        var text = string.Concat(Enumerable.Range(0, itemCount).Select(x => $@"---
uid: Test{x}
---

# Summary
yes summary{x}
# Remarks
yes remarks{x}
# Example
yes example{x} 1
# Example
yes example{x} 2
"));
        var (items, diagnostics) = ParseExtraDoc("path.md", text);
        Assert.NotNull(items);
        Assert.IsEmpty(diagnostics);
        Assert.AreEqual(itemCount, items.Count, $"Expecting {itemCount} item parsed");

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            Assert.AreEqual($"Test{i}", item.Name);
            Assert.IsTrue(item.IsExtraDoc);
            Assert.AreEqual("path.md", item.ExtraDocFilePath);
            Assert.AreEqual($"yes summary{i}", item.Summary?.Trim());
            Assert.AreEqual($"yes remarks{i}", item.Remarks?.Trim());

            Assert.NotNull(item.Examples);
            Assert.AreEqual(2, item.Examples.Count, "Expecting 2 examples parsed");
            Assert.AreEqual($"yes example{i} 1", item.Examples[0]?.Trim());
            Assert.AreEqual($"yes example{i} 2", item.Examples[1]?.Trim());
        }
    }


    [Test]
    public void TestParserNoFrontMatterValid()
    {
        var (items, diagnostics) = ParseExtraDoc("path.md", @"Invalid");
        Assert.Null(items);
        Assert.AreEqual(1, diagnostics.Count, "Expecting one diagnostic");
        StringAssert.Contains("Expecting YAML frontmatter", diagnostics[0].ToString());
    }

    [Test]
    public void TestParserFrontMatterNotClosed()
    {
        var (items, diagnostics) = ParseExtraDoc("path.md", "---\ninvalid");
        Assert.Null(items);
        Assert.AreEqual(1, diagnostics.Count, "Expecting one diagnostic");
        StringAssert.Contains("Invalid YAML frontmatter `---` at line: 1 not being closed", diagnostics[0].ToString());
    }

    private static (List<MetadataItem> items, List<Diagnostic> reports) ParseExtraDoc(string path, string text)
    {
        var analyzer = new ExtractorAnalyzer();

        var diagnostics = new List<Diagnostic>();
        var compilation = CSharpCompilation.Create("TestExtraDocParser");
#pragma warning disable CS0618
        var context = new CompilationAnalysisContext(compilation, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), diagnostic =>
        {
            diagnostics.Add(diagnostic);
        }, diagnostic => true, new CancellationToken());
#pragma warning restore CS0618


        var items = analyzer.TryParseExtraDoc(context, new InMemoryAdditionalText(path, text));


        foreach (var diag in diagnostics)
        {
            Console.WriteLine(diag);
        }

        return (items, diagnostics);
    }

    private class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;
        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }


        public override SourceText? GetText(CancellationToken cancellationToken = new CancellationToken())
        {
            return _text;
        }

        public override string Path { get; }
    }
}
