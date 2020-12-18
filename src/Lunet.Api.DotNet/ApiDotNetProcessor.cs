// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using DotNet.Globbing;
using Lunet.Api.DotNet.Extractor;
using Lunet.Core;
using Lunet.Json;
using Scriban.Functions;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Api.DotNet
{
    public class ApiDotNetProcessor : ProcessorBase<ApiDotNetPlugin>
    {
        private readonly string _customMsBuildFileProps;
        private const int MaxNumberOfPipeServers = 16;

        public ApiDotNetProcessor(ApiDotNetPlugin plugin, ApiDotNetConfig config) : base(plugin)
        {
            Config = config;
            _customMsBuildFileProps = Path.Combine(AppContext.BaseDirectory, "shared", "api", "dotnet", "Lunet.Api.DotNet.Extractor.props");
            Projects = new List<ApiDotNetProject>();
        }

        public ApiDotNetConfig Config { get; }
        
        public List<ApiDotNetProject> Projects { get; }
        
        public override void Process(ProcessingStage stage)
        {
            if (!File.Exists(_customMsBuildFileProps))
            {
                Site.Error($"Invalid api dotnet setup. The file {_customMsBuildFileProps} was not found");
                return;
            }

            if (Config.Projects == null || Config.Projects.Count == 0)
            {
                Site.Warning("No projects configured for api.dotnet");
                return;
            }

            GenerateApi();
            LoadProjects();
        }

        private void LoadProjects()
        {
            var cacheFs = GetCacheFileSystem();
            foreach (var project in Projects)
            {
                if (project.IsCachePathValid)
                {
                    var clock = Stopwatch.StartNew();
                    using var stream = cacheFs.OpenFile(project.CachePath, FileMode.Open, FileAccess.Read);
                    project.Api = (ScriptObject)JsonUtil.FromStream(stream, (string) project.CachePath);
                    Site.Info($"Api loaded for {project.Name} in {clock.Elapsed.TotalMilliseconds}ms {project.Api.Count} members");
                }
            }
        }

        FileSystem GetCacheFileSystem()
        {
            return Site.CacheSiteFileSystem.GetOrCreateSubFileSystem((UPath)"/api/dotnet");
        }
        
        private void GenerateApi()
        {
            var cachefs = GetCacheFileSystem();

            // Collect csproj
            var rootDirectory = Site.SiteFileSystem.ConvertPathToInternal(UPath.Root);
            foreach (var projectEntry in Config.Projects)
            {
                string projectName = null;
                var projectPath = projectEntry as string;
                ScriptObject projectProperties = null;

                // TODO: log error
                if (projectPath == null)
                {
                    var projectObject = projectEntry as ScriptObject;
                    if (projectObject != null)
                    {
                        projectName = projectObject["name"] as string;
                        projectPath = projectObject["path"] as string;
                        projectProperties = projectObject["properties"] as ScriptObject;
                    }
                }

                if (projectPath == null)
                {
                    Site.Error($"Invalid project description {projectEntry}. Expecting a string or an object with at least a path {{ path: '...' }}");
                    continue;
                }

                var entryPath = Path.GetFullPath(Path.Combine(rootDirectory, projectPath));
                if (File.Exists(entryPath))
                {
                    Projects.Add(new ApiDotNetProject()
                    {
                        Name = projectName ?? Path.GetFileNameWithoutExtension(entryPath),
                        Path = entryPath,
                        Properties = projectProperties
                    });
                }
                else
                {
                    var parentFolder = Path.GetDirectoryName(entryPath);
                    while (!Directory.Exists(parentFolder))
                    {
                        parentFolder = Path.GetDirectoryName(entryPath);
                        if (parentFolder == null)
                        {
                            break;
                        }
                    }

                    // TODO: log error
                    if (parentFolder == null) continue;


                    var globStr = entryPath.Substring(parentFolder.Length + 1);
                    var glob = Glob.Parse(globStr.StartsWith("**") ? globStr : $"**/{globStr}");

                    foreach (var entry in Directory.EnumerateFiles(parentFolder, "*.*proj", SearchOption.AllDirectories))
                    {
                        if (glob.IsMatch(entry))
                        {
                            Projects.Add(new ApiDotNetProject()
                            {
                                Name = Path.GetFileNameWithoutExtension(entry),
                                Path = entry,
                                Properties = projectProperties
                            });
                        }
                    }
                }
            }

            var sharedProperties = Config.Properties ?? new ScriptObject();

            // Clean and build each project
            foreach (var project in Projects)
            {
                var apiFileName = $"{project.Name}.api.json";
                var apiJson = (UPath)$"/{apiFileName}";

                project.CachePath = apiJson;
                var apiFileCached = cachefs.FileExists(apiJson);
                project.IsCachePathValid = apiFileCached;
                bool isRebuilding = !apiFileCached;

            rebuild:

                Site.Info(apiFileCached ?
                    $"The api dotnet `{apiFileName}` is already cached. Verifying changes for `{project.Name}`." :
                    $"Start building api dotnet for `{project.Name}`."
                );
                var clock = Stopwatch.StartNew();
                var buildProject = new DotNetProgram("msbuild")
                {
                    Arguments =
                    {
                        project.Path,
                        $"/t:{(isRebuilding?"Clean;":"")}Build"
                    },
                    WorkingDirectory = Path.GetDirectoryName(project.Path)
                };

                // Copy global properties
                foreach (var prop in sharedProperties)
                {
                    if (prop.Value == null) continue;
                    buildProject.Properties[prop.Key] = prop.Value;
                }

                // Overrides with the properties from the project
                if (project.Properties != null)
                {
                    foreach (var prop in project.Properties)
                    {
                        if (prop.Value == null) continue;
                        buildProject.Properties[prop.Key] = prop.Value;
                    }
                }

                // Make sure we have the last word
                buildProject.Properties["CustomBeforeMicrosoftCommonProps"] = _customMsBuildFileProps;
                buildProject.Properties["Configuration"] = Config.SolutionConfiguration ?? "Release";

                try
                {
                    var resultAsText = buildProject.Run();
                    //Site.Info($"Result: {resultAsText}");
                    var results = ExtractorHelper.FindResults(resultAsText);

                    Site.Info($"End {(apiFileCached ? "verifying" : "building")} api dotnet for `{project.Name}` completed in {clock.Elapsed.TotalMilliseconds}ms. {(results.Count == 0 ? "No new api files generated." : $"Generated new api files {results.Count}.")} ");

                    results.Sort();
                    if (results.Count == 0)
                    {
                        if (isRebuilding)
                        {
                            project.IsCachePathValid = false;
                            Site.Error($"Unable to build api dotnet for `{project.Name}`");
                        }
                        else if (!apiFileCached)
                        {
                            Site.Info($"Cache api dotnet for `{project.Name}` not available. Rebuilding.");
                            isRebuilding = true;
                            goto rebuild;
                        }
                        else
                        {
                            Site.Info($"The api dotnet for `{project.Name}` is up-to-date.");
                        }
                    }
                    else
                    {
                        if (results.Count > 1)
                        {
                            Site.Warning($"Multiple api dotnet output generated for `{project.Name}`. Consider setting TargetFramework in the config to use only one output. Using by default {results[0]}");
                        }

                        var file = results[0];
                        var sourceFs = new PhysicalFileSystem();
                        var sourcePath = sourceFs.ConvertPathFromInternal(file);

                        Site.Info($"Caching api dotnet {apiJson}");
                        sourceFs.CopyFileCross(sourcePath, cachefs, apiJson, true);
                        project.IsCachePathValid = true;
                    }
                }
                catch (Exception ex)
                {
                    Site.Error($"Error while building api dotnet for `{project.Name}`. Reason: {ex.Message}");
                }
            }
        }
    }
}