// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Metadata.ManagedReference;
using System.Xml;

namespace Lunet.Api.DotNet.Extractor
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, additionalLanguages: LanguageNames.VisualBasic)]
    public class ExtractorAnalyzer : DiagnosticAnalyzer
    {
        private const string LunetApiDotNet = "LunetApiDotNet";
        private static readonly DiagnosticDescriptor ResultDiagnostic = new DiagnosticDescriptor(id: ExtractorHelper.ResultId,
                                                                             title: "API Extractor Analyzer",
                                                                             messageFormat: "{0}",
                                                                             category: "Info",
                                                                             defaultSeverity: DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: true,
                                                                             description: "Result from API extractor",
                                                                             helpLinkUri: null,

                                                                             customTags: WellKnownDiagnosticTags.NotConfigurable);

        private static readonly DiagnosticDescriptor ErrorDiagnostic = new DiagnosticDescriptor(id: "XDoc0002",
            title: "Extractor Analyzer",
            messageFormat: "{0}",
            category: "Error",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Log from extractor Analyzer",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        private static readonly DiagnosticDescriptor WarningDiagnostic = new DiagnosticDescriptor(id: "XDoc0003",
            title: "Extractor Analyzer",
            messageFormat: "{0}",
            category: "Warning",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Warning from extractor analyzer",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(ResultDiagnostic, ErrorDiagnostic, WarningDiagnostic);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            //context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            //context.EnableConcurrentExecution();
            context.RegisterCompilationAction(CompileAction);
        }


        private static readonly object GlobalLock = new object();

        private void CompileAction(CompilationAnalysisContext context)
        {
            //lock (GlobalLock)
            {
                CompileActionInternal(context);
            }
        }
        
        private void CompileActionInternal(CompilationAnalysisContext context)
        {
            //context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue()

            //context.ReportDiagnostic(Diagnostic.Create(LoggerDiagnostic, null, $"Analyzer running"));

            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.generatedocumentationfile", out var generateDocumentationFileText))
            {
                return;
            }

            //context.ReportDiagnostic(Diagnostic.Create(LoggerDiagnostic, null, $"Analyzer generatedocumentationfile ok {generateDocumentationFileText}"));

            if (!string.Equals("true", generateDocumentationFileText, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectdir))
            {
                return;
            }

            //context.ReportDiagnostic(Diagnostic.Create(LoggerDiagnostic, null, $"Analyzer projectdir ok {projectdir}"));

            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.intermediateoutputpath", out var intermediateOutput))
            {
                return;
            }
            
            //context.ReportDiagnostic(Diagnostic.Create(LoggerDiagnostic, null, $"Analyzer intermediateoutputpath {intermediateOutput} ok"));

            var outputPath = Path.Combine(projectdir, intermediateOutput, $"{context.Compilation.AssemblyName}.api.json");
            var outputDir = Path.GetDirectoryName(outputPath);
            if (outputDir == null) return;
            Directory.CreateDirectory(outputDir);
            using var writer = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), new UTF8Encoding(false, false));

            //var clock = Stopwatch.StartNew();
            bool success = false;
            try
            {
                var compilation = WithXmlDocumentationForReferences(context.Compilation);

                // Collect additional doc files
                var extraDocs = new List<MetadataItem>();
                foreach (var file in context.Options.AdditionalFiles)
                {
                    if (context.Options.AnalyzerConfigOptionsProvider.GetOptions(file).TryGetValue($"build_metadata.AdditionalFiles.{LunetApiDotNet.ToLowerInvariant()}", out var text) && string.Compare(text, "true", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var itemsPerDoc = TryParseExtraDoc(context, file);
                        if (itemsPerDoc != null)
                        {
                            extraDocs.AddRange(itemsPerDoc);
                        }
                        else
                        {
                            context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, $"ApiDotNet File found {file.Path} but is not valid. Make sure that it contains a YAML frontmatter with a uid."));
                        }
                    }
                }

                var extractor = new RoslynMetadataExtractor(compilation);
                var metadata = extractor.Extract(new ExtractMetadataOptions()
                {
                    //CodeSourceBasePath = @"C:/code/Temp/SourceGenBugApp/SourceGenBugApp/"
                });
                
                var items = new List<MetadataItem>() {metadata};
                var includedReferenceAssemblies = GetIncludedReferenceAssemblies(context);
                ExtractSelectedReferenceAssemblies(context, compilation, items, includedReferenceAssemblies);
                // Add extra docs AFTER to allow MergeYamlProjectMetadata to work correctly
                metadata.Items.AddRange(extraDocs);

                var allMembers = MergeYamlProjectMetadata(context, items, includedReferenceAssemblies);
                var allReferences = MergeYamlProjectReferences(items);
                
                var model = YamlMetadataResolver.ResolveMetadata(allMembers, allReferences, true);

                var assemblyViewModel = new AssemblyViewModel()
                {
                    Name = context.Compilation.AssemblyName,
                    Items = model.Members
                        .Where(x => x.Type != MemberType.Default)
                        .Select(x => x.ToPageViewModel())
                        .ToList()
                };

                var serializer = new JsonSerializer(writer) {PrettyOutput = true};
                serializer.Write(assemblyViewModel);
                success = true;
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, $"Unexpected error while generating documentation: {ex} {ex.StackTrace}"));
            }
            writer.Flush();
            writer.Close();

            if (success)
            {
                context.ReportDiagnostic(Diagnostic.Create(ResultDiagnostic, null, ExtractorHelper.FormatResult(outputPath)));
            }
        }

        /// <summary>
        /// When running under <c>dotnet build</c> (no Workspace layer), Roslyn does not automatically attach
        /// XML documentation providers to metadata references. Attach providers when a sibling <c>.xml</c>
        /// file exists next to a referenced <c>.dll</c>.
        /// </summary>
        private static Compilation WithXmlDocumentationForReferences(Compilation compilation)
        {
            Compilation updated = compilation;
            Dictionary<string, DocumentationProvider> providersByXmlPath = null;

            foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
            {
                var dllPath = reference.FilePath;
                if (string.IsNullOrWhiteSpace(dllPath))
                {
                    continue;
                }

                var xmlPath = Path.ChangeExtension(dllPath, ".xml");
                if (!File.Exists(xmlPath))
                {
                    continue;
                }

                if (providersByXmlPath == null)
                {
                    providersByXmlPath = new Dictionary<string, DocumentationProvider>(StringComparer.OrdinalIgnoreCase);
                }

                if (!providersByXmlPath.TryGetValue(xmlPath, out var provider))
                {
                    provider = new XmlFileDocumentationProvider(xmlPath);
                    providersByXmlPath.Add(xmlPath, provider);
                }

                var updatedReference = MetadataReference.CreateFromFile(dllPath, reference.Properties, provider);
                updated = updated.ReplaceReference(reference, updatedReference);
            }

            return updated;
        }

        private sealed class XmlFileDocumentationProvider : DocumentationProvider
        {
            private readonly string _path;
            private Dictionary<string, string> _byId;

            public XmlFileDocumentationProvider(string path)
            {
                _path = path ?? throw new ArgumentNullException(nameof(path));
            }

            public override bool Equals(object obj)
            {
                return obj is XmlFileDocumentationProvider other && StringComparer.OrdinalIgnoreCase.Equals(_path, other._path);
            }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(_path);
            }

            protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrEmpty(documentationMemberID))
                {
                    return string.Empty;
                }

                EnsureLoaded(cancellationToken);

                return _byId.TryGetValue(documentationMemberID, out var xml) ? xml : string.Empty;
            }

            private void EnsureLoaded(CancellationToken cancellationToken)
            {
                if (_byId != null)
                {
                    return;
                }

                var map = new Dictionary<string, string>(StringComparer.Ordinal);

                try
                {
                    using (var stream = File.OpenRead(_path))
                    using (var reader = XmlReader.Create(stream, new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        IgnoreComments = true,
                        IgnoreProcessingInstructions = true,
                        IgnoreWhitespace = true,
                    }))
                    {
                        while (reader.Read())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType != XmlNodeType.Element || reader.Name != "member")
                            {
                                continue;
                            }

                            var name = reader.GetAttribute("name");
                            if (string.IsNullOrEmpty(name))
                            {
                                continue;
                            }

                            var content = reader.ReadOuterXml();
                            if (!map.ContainsKey(name))
                            {
                                map.Add(name, content);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore invalid or missing XML docs.
                }

                _byId = map;
            }
        }


        private enum ParseExtraDocState
        {
            None,
            InFrontMatter,
            InDoc,
        }

        public List<MetadataItem> TryParseExtraDoc(CompilationAnalysisContext context, AdditionalText file)
        {
            var text = file.GetText()?.ToString();
            if (text == null) return null;
            var reader = new StringReader(text);

            string currentSection = null;
            var builder = new StringBuilder();

            var state = ParseExtraDocState.None;

            List<MetadataItem> items = null;
            string line;
            MetadataItem item = null;
            int lastLineFrontMatter = 0;
            int lineCount = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineCount++;
                if (line == "---")
                {
                    if (state == ParseExtraDocState.InFrontMatter)
                    {
                        state = item == null ? ParseExtraDocState.None : ParseExtraDocState.InDoc;
                    }
                    else
                    {
                        if (item != null)
                        {
                            ProcessSection(currentSection, builder, item);
                        }

                        state = ParseExtraDocState.InFrontMatter;
                        lastLineFrontMatter = lineCount;
                        currentSection = null;
                        builder.Clear();
                        item = null;
                    }
                    continue;
                }

                switch (state)
                {
                    case ParseExtraDocState.None:
                        // If the file does not contain 
                        context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, $"Expecting YAML frontmatter `---` at the beginning of the extra documentation file {file.Path}"));
                        return null;

                    case ParseExtraDocState.InFrontMatter:
                        if (line.StartsWith("uid:") && item == null)
                        {
                            var uid = line.Substring("uid:".Length).Trim();
                            item = new MetadataItem { Name = uid, IsExtraDoc = true, ExtraDocFilePath = file.Path };
                            items ??= new List<MetadataItem>();
                            items.Add(item);
                        }
                        break;
                    case ParseExtraDocState.InDoc:

                        if (line.StartsWith("# "))
                        {
                            ProcessSection(currentSection, builder, item);
                            currentSection = line.Substring("# ".Length).Trim().ToLowerInvariant();
                        }
                        else if (currentSection != null)
                        {
                            builder.Append(line).Append('\n');
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            ProcessSection(currentSection, builder, item);

            if (state == ParseExtraDocState.InFrontMatter)
            {
                // If has an invalid non-closing front-matter
                context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, $"Invalid YAML frontmatter `---` at line: {lastLineFrontMatter} not being closed in the extra documentation file {file.Path}"));
                return null;
            }

            return items;
        }

        private void ProcessSection(string name, StringBuilder builder, MetadataItem item)
        {
            if (name == null) return;
            switch (name)
            {
                case "summary":
                    item.Summary = builder.ToString();
                    break;
                case "remarks":
                    item.Remarks = builder.ToString();
                    break;
                case "example":
                    if (item.Examples == null) item.Examples = new List<string>();
                    item.Examples.Add(builder.ToString());
                    break;
            }
            builder.Clear();
        }

        private void ExtractSelectedReferenceAssemblies(CompilationAnalysisContext context, Compilation compilation, List<MetadataItem> items, IReadOnlyCollection<string> includedReferenceAssemblies)
        {
            if (includedReferenceAssemblies == null || includedReferenceAssemblies.Count == 0)
            {
                return;
            }

            var requestedAssemblyNames = new HashSet<string>(includedReferenceAssemblies, StringComparer.OrdinalIgnoreCase);
            if (requestedAssemblyNames.Count == 0)
            {
                return;
            }

            foreach (var reference in compilation.References)
            {
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol == null)
                {
                    continue;
                }

                if (requestedAssemblyNames.Remove(assemblySymbol.Name) is false)
                {
                    continue;
                }

                try
                {
                    var referencedExtractor = new RoslynMetadataExtractor(compilation, assemblySymbol);
                    var referenceMetadata = referencedExtractor.Extract(new ExtractMetadataOptions());
                    items.Add(referenceMetadata);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, $"Unexpected error while generating metadata for referenced assembly `{assemblySymbol.Name}`: {ex}"));
                }
            }

            foreach (var assemblyName in requestedAssemblyNames)
            {
                context.ReportDiagnostic(Diagnostic.Create(WarningDiagnostic, null, $"Unable to find referenced assembly `{assemblyName}` in current compilation references."));
            }
        }

        private static HashSet<string> GetIncludedReferenceAssemblies(CompilationAnalysisContext context)
        {
            if (context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.lunetapidotnetincludeassemblies", out var rawAssemblyNames) is false || string.IsNullOrWhiteSpace(rawAssemblyNames))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return ParseRequestedAssemblyNames(rawAssemblyNames);
        }

        private static HashSet<string> ParseRequestedAssemblyNames(string rawAssemblyNames)
        {
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            rawAssemblyNames = DecodeMsBuildPropertyValue(rawAssemblyNames);
            var splits = rawAssemblyNames.Split(new[] {';', ',', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawAssemblyName in splits)
            {
                var assemblyName = NormalizeAssemblyName(rawAssemblyName);
                if (assemblyName != null)
                {
                    assemblyNames.Add(assemblyName);
                }
            }

            return assemblyNames;
        }

        private static string DecodeMsBuildPropertyValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = value.Trim();
            if (value.IndexOf('%') < 0)
            {
                return value;
            }

            var decoded = value;
            for (var i = 0; i < 2 && decoded.IndexOf('%') >= 0; i++)
            {
                try
                {
                    var candidate = Uri.UnescapeDataString(decoded);
                    if (candidate == decoded)
                    {
                        break;
                    }

                    decoded = candidate;
                }
                catch
                {
                    break;
                }
            }

            return decoded;
        }

        private static string NormalizeAssemblyName(string rawAssemblyName)
        {
            if (string.IsNullOrWhiteSpace(rawAssemblyName))
            {
                return null;
            }

            var assemblyName = rawAssemblyName.Trim();
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = Path.GetFileNameWithoutExtension(assemblyName);
            }

            return string.IsNullOrWhiteSpace(assemblyName) ? null : assemblyName;
        }

        private static Dictionary<string, MetadataItem> MergeYamlProjectMetadata(CompilationAnalysisContext context, List<MetadataItem> projectMetadataList, IReadOnlyCollection<string> includedReferenceAssemblies)
        {
            if (projectMetadataList == null || projectMetadataList.Count == 0)
            {
                return null;
            }

            Dictionary<string, MetadataItem> namespaceMapping = new Dictionary<string, MetadataItem>();
            Dictionary<string, MetadataItem> allMembers = new Dictionary<string, MetadataItem>();
            List<MetadataItem> pendingExtraDocs = new List<MetadataItem>();

            foreach (var project in projectMetadataList)
            {
                if (project.Items != null)
                {
                    foreach (var ns in project.Items)
                    {
                        if (ns.IsExtraDoc)
                        {
                            pendingExtraDocs.Add(ns);
                            continue;
                        }

                        if (ns.Type == MemberType.Namespace)
                        {
                            if (namespaceMapping.TryGetValue(ns.Name, out MetadataItem nsOther))
                            {
                                if (ns.Items != null)
                                {
                                    if (nsOther.Items == null)
                                    {
                                        nsOther.Items = new List<MetadataItem>();
                                    }

                                    foreach (var i in ns.Items)
                                    {
                                        if (!nsOther.Items.Any(s => s.Name == i.Name))
                                        {
                                            nsOther.Items.Add(i);
                                        }
                                        else
                                        {
                                            Logger.Log(LogLevel.Info, $"{i.Name} already exists in {nsOther.Name}, ignore current one");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                namespaceMapping.Add(ns.Name, ns);
                            }
                        }

                        if (!allMembers.ContainsKey(ns.Name))
                        {
                            allMembers.Add(ns.Name, ns);
                        }

                        ns.Items?.ForEach(s =>
                        {
                            if (allMembers.TryGetValue(s.Name, out MetadataItem existingMetadata))
                            {
                                Logger.Log(LogLevel.Warning, $"Duplicate member {s.Name} is found from {existingMetadata.Source.Path} and {s.Source.Path}, use the one in {existingMetadata.Source.Path} and ignore the one from {s.Source.Path}");
                            }
                            else
                            {
                                allMembers.Add(s.Name, s);
                            }

                            s.Items?.ForEach(s1 =>
                            {
                                if (allMembers.TryGetValue(s1.Name, out MetadataItem existingMetadata1))
                                {
                                    Logger.Log(LogLevel.Warning, $"Duplicate member {s1.Name} is found from {existingMetadata1.Source.Path} and {s1.Source.Path}, use the one in {existingMetadata1.Source.Path} and ignore the one from {s1.Source.Path}");
                                }
                                else
                                {
                                    allMembers.Add(s1.Name, s1);
                                }
                            });
                        });
                    }
                }
            }

            foreach (var extraDoc in pendingExtraDocs)
            {
                if (allMembers.TryGetValue(extraDoc.Name, out var existingItem))
                {
                    MergeExtraDocumentation(existingItem, extraDoc);
                }
                else
                {
                    var message = $"Unexpected uid {extraDoc.Name} found in extra documentation file `{extraDoc.ExtraDocFilePath}`. The uid does not match any existing documentation item.";
                    if (IsLikelyExternalUid(extraDoc.Name, includedReferenceAssemblies))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(WarningDiagnostic, null, message));
                    }
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, message));
                    }
                }
            }

            return allMembers;
        }

        private static void MergeExtraDocumentation(MetadataItem existingItem, MetadataItem extraDoc)
        {
            existingItem.Summary = ConcatDoc(existingItem.Summary, extraDoc.Summary);
            existingItem.Remarks = ConcatDoc(existingItem.Remarks, extraDoc.Remarks);
            if (extraDoc.Examples != null)
            {
                if (existingItem.Examples == null)
                {
                    existingItem.Examples = extraDoc.Examples;
                }
                else
                {
                    existingItem.Examples.AddRange(extraDoc.Examples);
                }
            }
        }

        private static bool IsLikelyExternalUid(string uid, IReadOnlyCollection<string> includedReferenceAssemblies)
        {
            if (string.IsNullOrWhiteSpace(uid) || includedReferenceAssemblies == null || includedReferenceAssemblies.Count == 0)
            {
                return false;
            }

            foreach (var assemblyName in includedReferenceAssemblies)
            {
                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    continue;
                }

                if (uid.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) || uid.StartsWith(assemblyName + ".", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ConcatDoc(string left, string right)
        {
            if (left == null && right == null) return null;
            if (left == null) return right;
            if (right == null) return left;
            return $"{left}\n{right}";
        }

        private static Dictionary<string, ReferenceItem> MergeYamlProjectReferences(List<MetadataItem> projectMetadataList)
        {
            if (projectMetadataList == null || projectMetadataList.Count == 0)
            {
                return null;
            }

            var result = new Dictionary<string, ReferenceItem>();

            foreach (var project in projectMetadataList)
            {
                if (project.References != null)
                {
                    foreach (var pair in project.References)
                    {
                        if (!result.ContainsKey(pair.Key))
                        {
                            result[pair.Key] = pair.Value;
                        }
                        else
                        {
                            result[pair.Key].Merge(pair.Value);
                        }
                    }
                }
            }

            return result;
        }

        //public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new ImmutableArray<DiagnosticDescriptor>();
    }
}
