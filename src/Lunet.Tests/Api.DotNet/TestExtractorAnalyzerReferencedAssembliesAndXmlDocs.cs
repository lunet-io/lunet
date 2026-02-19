// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Lunet.Api.DotNet.Extractor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Lunet.Tests.Api.DotNet;

[NonParallelizable]
public class TestExtractorAnalyzerReferencedAssembliesAndXmlDocs
{
    [Test]
    public void TestExtractorDecodesEncodedIncludeAssembliesAndLoadsXmlDocs()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"lunet-extractor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var references = BuildMinimalMetadataReferences();

            var refOnePath = Path.Combine(rootDirectory, "RefOne.dll");
            var refTwoPath = Path.Combine(rootDirectory, "RefTwo.dll");

            EmitAssembly(
                assemblyName: "RefOne",
                outputPath: refOnePath,
                references: references,
                source: """
                        namespace RefOne;

                        /// <summary>RefOneType summary from XML.</summary>
                        public sealed class RefOneType
                        {
                        }
                        """);

            EmitAssembly(
                assemblyName: "RefTwo",
                outputPath: refTwoPath,
                references: references,
                source: """
                        namespace RefTwo;

                        /// <summary>RefTwoType summary from XML.</summary>
                        public sealed class RefTwoType
                        {
                        }
                        """);

            var mainCompilation = CSharpCompilation.Create(
                "MainAssembly",
                syntaxTrees:
                [
                    CSharpSyntaxTree.ParseText(
                        """
                        namespace MainAssembly;

                        public static class Dummy
                        {
                        }
                        """,
                        new CSharpParseOptions(languageVersion: LanguageVersion.Preview))
                ],
                references: references.Concat(
                [
                    MetadataReference.CreateFromFile(refOnePath),
                    MetadataReference.CreateFromFile(refTwoPath),
                ]),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            var diagnostics = new List<Diagnostic>();
            var analyzer = new ExtractorAnalyzer();

            var outputObjectDir = Path.Combine(rootDirectory, "obj");
            var globalOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_property.generatedocumentationfile"] = "true",
                ["build_property.projectdir"] = rootDirectory,
                ["build_property.intermediateoutputpath"] = "obj\\",
                ["build_property.lunetapidotnetincludeassemblies"] = "RefOne%253BRefTwo",
            };

            var analyzerOptions = new AnalyzerOptions(
                ImmutableArray<AdditionalText>.Empty,
                new InMemoryAnalyzerConfigOptionsProvider(globalOptions));

#pragma warning disable CS0618
            var compilationContext = new CompilationAnalysisContext(
                mainCompilation,
                analyzerOptions,
                reportDiagnostic: diagnostic => diagnostics.Add(diagnostic),
                isSupportedDiagnostic: _ => true,
                cancellationToken: CancellationToken.None);
#pragma warning restore CS0618

            var compileActionInternal = typeof(ExtractorAnalyzer).GetMethod("CompileActionInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(compileActionInternal, "Unable to locate ExtractorAnalyzer.CompileActionInternal.");
            compileActionInternal!.Invoke(analyzer, [compilationContext]);

            var outputPath = Path.Combine(outputObjectDir, "MainAssembly.api.json");
            Assert.IsTrue(File.Exists(outputPath), $"Expected extractor output `{outputPath}`.");

            using var json = JsonDocument.Parse(File.ReadAllText(outputPath, Encoding.UTF8));
            var refOneType = FindBestItemByUid(json.RootElement, "RefOne.RefOneType");
            var refTwoType = FindBestItemByUid(json.RootElement, "RefTwo.RefTwoType");

            Assert.That(refOneType.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
            Assert.That(refTwoType.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
            Assert.IsTrue(refOneType.TryGetProperty("type", out var refOneTypeProperty), $"Expected `type` for `RefOne.RefOneType`, but got: {refOneType}");
            Assert.IsTrue(refTwoType.TryGetProperty("type", out var refTwoTypeProperty), $"Expected `type` for `RefTwo.RefTwoType`, but got: {refTwoType}");
            Assert.That(refOneTypeProperty.GetString(), Is.EqualTo("Class"));
            Assert.That(refTwoTypeProperty.GetString(), Is.EqualTo("Class"));

            Assert.IsTrue(refOneType.TryGetProperty("summary", out var refOneSummaryProperty), $"Expected `summary` for `RefOne.RefOneType`, but got: {refOneType}");
            Assert.IsTrue(refTwoType.TryGetProperty("summary", out var refTwoSummaryProperty), $"Expected `summary` for `RefTwo.RefTwoType`, but got: {refTwoType}");

            var refOneSummary = refOneSummaryProperty.GetString() ?? string.Empty;
            var refTwoSummary = refTwoSummaryProperty.GetString() ?? string.Empty;

            StringAssert.Contains("RefOneType summary from XML.", refOneSummary);
            StringAssert.Contains("RefTwoType summary from XML.", refTwoSummary);

            Assert.That(
                diagnostics.Select(d => d.ToString()),
                Has.None.Contains("Unable to find referenced assembly"),
                "Referenced assemblies should be resolved after decoding %253B.");
        }
        finally
        {
            try
            {
                Directory.Delete(rootDirectory, true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void TestExtractorDoesNotFailWhenExtraDocForIncludedExternalUidIsMissing()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"lunet-extractor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var references = BuildMinimalMetadataReferences();
            var refOnePath = Path.Combine(rootDirectory, "RefOne.dll");
            EmitAssembly(
                assemblyName: "RefOne",
                outputPath: refOnePath,
                references: references,
                source: """
                        namespace RefOne;

                        public sealed class RefOneType
                        {
                        }
                        """);

            var missingExtraDocPath = Path.Combine(rootDirectory, "RefOne.Internal.md");
            const string missingExtraDocContent = """
                                                  ---
                                                  uid: RefOne.Internal
                                                  ---

                                                  # Summary
                                                  Extra docs for a non-public or missing external namespace.
                                                  """;

            var mainCompilation = CSharpCompilation.Create(
                "MainAssembly",
                syntaxTrees:
                [
                    CSharpSyntaxTree.ParseText(
                        """
                        namespace MainAssembly;

                        public static class Dummy
                        {
                        }
                        """,
                        new CSharpParseOptions(languageVersion: LanguageVersion.Preview))
                ],
                references:
                [
                    .. references,
                    MetadataReference.CreateFromFile(refOnePath),
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            var diagnostics = new List<Diagnostic>();
            var analyzer = new ExtractorAnalyzer();
            var outputObjectDir = Path.Combine(rootDirectory, "obj");
            var globalOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_property.generatedocumentationfile"] = "true",
                ["build_property.projectdir"] = rootDirectory,
                ["build_property.intermediateoutputpath"] = "obj\\",
                ["build_property.lunetapidotnetincludeassemblies"] = "RefOne",
                ["build_metadata.AdditionalFiles.lunetapidotnet"] = "true",
            };

            var analyzerOptions = new AnalyzerOptions(
                [new InMemoryAdditionalText(missingExtraDocPath, missingExtraDocContent)],
                new InMemoryAnalyzerConfigOptionsProvider(globalOptions));

#pragma warning disable CS0618
            var compilationContext = new CompilationAnalysisContext(
                mainCompilation,
                analyzerOptions,
                reportDiagnostic: diagnostic => diagnostics.Add(diagnostic),
                isSupportedDiagnostic: _ => true,
                cancellationToken: CancellationToken.None);
#pragma warning restore CS0618

            var compileActionInternal = typeof(ExtractorAnalyzer).GetMethod("CompileActionInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(compileActionInternal, "Unable to locate ExtractorAnalyzer.CompileActionInternal.");
            compileActionInternal!.Invoke(analyzer, [compilationContext]);

            Assert.That(
                diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => x.GetMessage()),
                Has.None.Contains("Unexpected uid RefOne.Internal"),
                "Missing external extra-doc UIDs should not fail the build.");

            Assert.That(
                diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).Select(x => x.GetMessage()),
                Has.Some.Contains("Unexpected uid RefOne.Internal"),
                "Missing external extra-doc UIDs should emit a warning.");

            var outputPath = Path.Combine(outputObjectDir, "MainAssembly.api.json");
            Assert.IsTrue(File.Exists(outputPath), $"Expected extractor output `{outputPath}`.");
        }
        finally
        {
            try
            {
                Directory.Delete(rootDirectory, true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void TestExtractorMergesExtraDocForReferencedNamespace()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"lunet-extractor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var references = BuildMinimalMetadataReferences();
            var refOnePath = Path.Combine(rootDirectory, "RefOne.dll");

            EmitAssembly(
                assemblyName: "RefOne",
                outputPath: refOnePath,
                references: references,
                source: """
                        namespace RefOne;

                        /// <summary>RefOneType summary from XML.</summary>
                        public sealed class RefOneType
                        {
                        }
                        """);

            var extraDocPath = Path.Combine(rootDirectory, "RefOne.md");
            const string extraDocContent = """
                                           ---
                                           uid: RefOne
                                           ---

                                           # Summary
                                           Extra summary for referenced namespace.
                                           """;

            var mainCompilation = CSharpCompilation.Create(
                "MainAssembly",
                syntaxTrees:
                [
                    CSharpSyntaxTree.ParseText(
                        """
                        namespace MainAssembly;

                        public static class Dummy
                        {
                        }
                        """,
                        new CSharpParseOptions(languageVersion: LanguageVersion.Preview))
                ],
                references:
                [
                    .. references,
                    MetadataReference.CreateFromFile(refOnePath),
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            var diagnostics = new List<Diagnostic>();
            var analyzer = new ExtractorAnalyzer();
            var outputObjectDir = Path.Combine(rootDirectory, "obj");
            var globalOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_property.generatedocumentationfile"] = "true",
                ["build_property.projectdir"] = rootDirectory,
                ["build_property.intermediateoutputpath"] = "obj\\",
                ["build_property.lunetapidotnetincludeassemblies"] = "RefOne",
                ["build_metadata.AdditionalFiles.lunetapidotnet"] = "true",
            };

            var analyzerOptions = new AnalyzerOptions(
                [new InMemoryAdditionalText(extraDocPath, extraDocContent)],
                new InMemoryAnalyzerConfigOptionsProvider(globalOptions));

#pragma warning disable CS0618
            var compilationContext = new CompilationAnalysisContext(
                mainCompilation,
                analyzerOptions,
                reportDiagnostic: diagnostic => diagnostics.Add(diagnostic),
                isSupportedDiagnostic: _ => true,
                cancellationToken: CancellationToken.None);
#pragma warning restore CS0618

            var compileActionInternal = typeof(ExtractorAnalyzer).GetMethod("CompileActionInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(compileActionInternal, "Unable to locate ExtractorAnalyzer.CompileActionInternal.");
            compileActionInternal!.Invoke(analyzer, [compilationContext]);

            Assert.That(
                diagnostics.Select(d => d.GetMessage()),
                Has.None.Contains("Unexpected uid RefOne"),
                "Referenced namespace extra documentation should merge instead of failing.");

            var outputPath = Path.Combine(outputObjectDir, "MainAssembly.api.json");
            Assert.IsTrue(File.Exists(outputPath), $"Expected extractor output `{outputPath}`.");

            using var json = JsonDocument.Parse(File.ReadAllText(outputPath, Encoding.UTF8));
            var referencedNamespace = FindBestItemByUid(json.RootElement, "RefOne");
            var referencedType = FindBestItemByUid(json.RootElement, "RefOne.RefOneType");

            Assert.That(referencedNamespace.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
            Assert.That(referencedType.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
            Assert.IsTrue(referencedType.TryGetProperty("type", out var referencedTypeProperty), $"Expected `type` for `RefOne.RefOneType`, but got: {referencedType}");
            Assert.That(referencedTypeProperty.GetString(), Is.EqualTo("Class"));
            Assert.IsTrue(referencedNamespace.TryGetProperty("summary", out var namespaceSummaryProperty), $"Expected `summary` for `RefOne`, but got: {referencedNamespace}");

            var namespaceSummary = namespaceSummaryProperty.GetString() ?? string.Empty;
            StringAssert.Contains("Extra summary for referenced namespace.", namespaceSummary);
        }
        finally
        {
            try
            {
                Directory.Delete(rootDirectory, true);
            }
            catch
            {
            }
        }
    }

    private static IReadOnlyList<MetadataReference> BuildMinimalMetadataReferences()
    {
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("Unable to resolve TRUSTED_PLATFORM_ASSEMBLIES for metadata references.");
        }

        var byFileName = trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(path => Path.GetFileName(path), path => path, StringComparer.OrdinalIgnoreCase);

        var required = new[]
        {
            "System.Private.CoreLib.dll",
            "System.Runtime.dll",
            "netstandard.dll",
        };

        var result = new List<MetadataReference>(required.Length);
        foreach (var fileName in required)
        {
            if (!byFileName.TryGetValue(fileName, out var path))
            {
                throw new InvalidOperationException($"Unable to locate `{fileName}` in TRUSTED_PLATFORM_ASSEMBLIES.");
            }

            result.Add(MetadataReference.CreateFromFile(path));
        }

        return result;
    }

    private static void EmitAssembly(string assemblyName, string outputPath, IReadOnlyList<MetadataReference> references, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion: LanguageVersion.Preview))],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var xmlPath = Path.ChangeExtension(outputPath, ".xml");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var dllStream = File.Create(outputPath);
        using var xmlStream = File.Create(xmlPath);
        var emitResult = compilation.Emit(dllStream, xmlDocumentationStream: xmlStream);
        if (!emitResult.Success)
        {
            var messages = string.Join(Environment.NewLine, emitResult.Diagnostics);
            Assert.Fail($"Failed to emit `{assemblyName}`: {Environment.NewLine}{messages}");
        }
    }

    private static JsonElement FindBestItemByUid(JsonElement element, string uid)
    {
        var matches = new List<JsonElement>();
        CollectByUid(element, uid, matches);

        if (matches.Count == 0)
        {
            return default;
        }

        foreach (var match in matches)
        {
            if (match.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (match.TryGetProperty("summary", out _) || match.TryGetProperty("type", out _))
            {
                return match;
            }
        }

        return matches[0];
    }

    private static void CollectByUid(JsonElement element, string uid, List<JsonElement> matches)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("uid", out var uidProperty) && uidProperty.ValueKind == JsonValueKind.String)
            {
                var value = uidProperty.GetString();
                if (string.Equals(uid, value, StringComparison.Ordinal))
                {
                    matches.Add(element);
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                CollectByUid(property.Value, uid, matches);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectByUid(item, uid, matches);
            }
        }
    }

    private sealed class InMemoryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions;

        public InMemoryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
        {
            _globalOptions = new InMemoryAnalyzerConfigOptions(globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;
    }

    private sealed class InMemoryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _options;

        public InMemoryAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _content;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override Microsoft.CodeAnalysis.Text.SourceText GetText(CancellationToken cancellationToken = default)
        {
            return Microsoft.CodeAnalysis.Text.SourceText.From(_content, Encoding.UTF8);
        }
    }
}
