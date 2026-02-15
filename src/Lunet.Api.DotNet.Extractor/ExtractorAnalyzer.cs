// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Metadata.ManagedReference;

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

        private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(ResultDiagnostic, ErrorDiagnostic);

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

                var extractor = new RoslynMetadataExtractor(context.Compilation);
                var metadata = extractor.Extract(new ExtractMetadataOptions()
                {
                    //CodeSourceBasePath = @"C:/code/Temp/SourceGenBugApp/SourceGenBugApp/"
                });
                
                var items = new List<MetadataItem>() {metadata};
                // Add extra docs AFTER to allow MergeYamlProjectMetadata to work correctly
                metadata.Items.AddRange(extraDocs);

                var allMembers = MergeYamlProjectMetadata(context, items);
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

        private static Dictionary<string, MetadataItem> MergeYamlProjectMetadata(CompilationAnalysisContext context, List<MetadataItem> projectMetadataList)
        {
            if (projectMetadataList == null || projectMetadataList.Count == 0)
            {
                return null;
            }

            Dictionary<string, MetadataItem> namespaceMapping = new Dictionary<string, MetadataItem>();
            Dictionary<string, MetadataItem> allMembers = new Dictionary<string, MetadataItem>();

            foreach (var project in projectMetadataList)
            {
                if (project.Items != null)
                {
                    foreach (var ns in project.Items)
                    {
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

                        if (allMembers.TryGetValue(ns.Name, out var existingNs))
                        {
                            // Merge the extra documentation
                            if (ns.IsExtraDoc)
                            {
                                existingNs.Summary = ConcatDoc(existingNs.Summary, ns.Summary);
                                existingNs.Remarks = ConcatDoc(existingNs.Remarks, ns.Remarks);
                                if (ns.Examples != null)
                                {
                                    if (existingNs.Examples == null)
                                    {
                                        existingNs.Examples = ns.Examples;
                                    }
                                    else
                                    {
                                        existingNs.Examples.AddRange(ns.Examples);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (ns.IsExtraDoc)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ErrorDiagnostic, null, $"Unexpected uid {ns.Name} found in extra documentation file `{ns.ExtraDocFilePath}`. The uid does not match any existing documentation item."));
                            }
                            else
                            {
                                allMembers.Add(ns.Name, ns);
                            }
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

            return allMembers;
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
