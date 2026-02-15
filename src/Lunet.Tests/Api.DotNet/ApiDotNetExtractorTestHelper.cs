// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Metadata.ManagedReference;

namespace Lunet.Tests.Api.DotNet;

internal static class ApiDotNetExtractorTestHelper
{
    private static readonly MetadataReference[] MetadataReferences = BuildMetadataReferences();

    public static MetadataItem ExtractSingleFile(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(languageVersion: LanguageVersion.Preview),
            path: "ApiFeatureSample.cs");

        var compilation = CSharpCompilation.Create(
            "ApiFeatureTests",
            [syntaxTree],
            MetadataReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));

        var compilationErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (compilationErrors.Count > 0)
        {
            Assert.Fail($"Unexpected compilation error(s): {Environment.NewLine}{string.Join(Environment.NewLine, compilationErrors)}");
        }

        var extractor = new RoslynMetadataExtractor(compilation);
        return extractor.Extract(new ExtractMetadataOptions());
    }

    public static MetadataItem FindSingleByTypeAndUidContains(MetadataItem root, MemberType memberType, string uidFragment)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(uidFragment);

        var matches = Flatten(root)
            .Where(item => item.Type == memberType)
            .Where(item => item.Name.Contains(uidFragment, StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(1, matches.Count, $"Expected a single `{memberType}` item containing `{uidFragment}`. Found: {matches.Count}");
        return matches[0];
    }

    public static string GetCSharpSyntax(MetadataItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Assert.NotNull(item.Syntax);
        Assert.NotNull(item.Syntax!.Content);
        if (item.Syntax.Content.TryGetValue(SyntaxLanguage.CSharp, out var csharpSyntax) && csharpSyntax is not null)
        {
            return csharpSyntax;
        }

        if (item.Syntax.Content.TryGetValue(SyntaxLanguage.Default, out var defaultSyntax) && defaultSyntax is not null)
        {
            return defaultSyntax;
        }

        return string.Empty;
    }

    public static List<string> GetCSharpModifiers(MetadataItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Modifiers.TryGetValue(SyntaxLanguage.CSharp, out var modifiers) && modifiers is not null)
        {
            return modifiers;
        }

        return [];
    }

    private static List<MetadataItem> Flatten(MetadataItem root)
    {
        var result = new List<MetadataItem>();
        FlattenCore(root, result);
        return result;
    }

    private static void FlattenCore(MetadataItem? item, List<MetadataItem> output)
    {
        if (item is null)
        {
            return;
        }

        output.Add(item);
        if (item.Items is null)
        {
            return;
        }

        foreach (var child in item.Items)
        {
            FlattenCore(child, output);
        }
    }

    private static MetadataReference[] BuildMetadataReferences()
    {
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("Unable to resolve TRUSTED_PLATFORM_ASSEMBLIES for metadata references.");
        }

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
