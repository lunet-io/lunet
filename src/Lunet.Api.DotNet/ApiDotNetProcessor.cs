// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Autofac.Core.Activators;
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
            _customMsBuildFileProps = Path.Combine(AppContext.BaseDirectory, SiteFileSystems.SharedFolderName, SiteFileSystems.LunetFolderName, SiteFileSystems.ModulesFolderName, "api", "dotnet", "Lunet.Api.DotNet.Extractor.props");
            Cache = new ApiDotNetCache();
            Projects = new List<ApiDotNetProject>();
        }

        public ApiDotNetConfig Config { get; }
        
        public List<ApiDotNetProject> Projects { get; }
        
        public ApiDotNetCache Cache { get; }

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

            CollectProjectsFromConfiguration();
            
            BuildProjectsAndGenerateApi();

            if (LoadGeneratedApis())
            {
                UpdateUid();
            }
        }

        private void UpdateUid()
        {
            var namespaces = new Dictionary<string, ScriptObject>();
            
            foreach (var project in Projects)
            {
                if (project.Api == null) continue;

                var objects = project.Api.GetSafeValue<ScriptArray>("items");

                foreach (var obj in objects.OfType<ScriptObject>().SelectMany(x => (ScriptArray)x["items"]).OfType<ScriptObject>())
                {
                    var uid = (string) obj["uid"];
                    if (obj.GetSafeValue<string>("type") == "Namespace")
                    {
                        ScriptObject nsScriptObject;
                        if (!Cache.Objects.TryGetValue(uid, out var nsObject))
                        {
                            nsObject = new ScriptObject();
                            nsScriptObject = (ScriptObject)nsObject;
                            namespaces.Add(uid, nsScriptObject);
                            nsScriptObject.Add("uid", uid);
                            nsScriptObject.Add("commentId", obj["commentId"]);
                            nsScriptObject.Add("id", obj["id"]);
                            nsScriptObject.Add("name", obj["name"]);
                            nsScriptObject.Add("nameWithType", obj["nameWithType"]);
                            nsScriptObject.Add("fullName", obj["fullName"]);
                            nsScriptObject.Add("type", obj["type"]);
                            nsScriptObject.Add("children", new ScriptArray());
                            nsScriptObject.Add("assemblies", new ScriptArray());
                            nsScriptObject.Add("langs", new ScriptArray());
                            Cache.Objects.Add(uid, nsObject);
                        }
                        else
                        {
                            nsScriptObject = (ScriptObject)nsObject;
                        }

                        // Merge child types
                        var nsChildren = nsScriptObject.GetSafeValue<ScriptArray>("children");
                        var nsChildrenConcat = nsScriptObject.GetSafeValue<ScriptArray>("children").OfType<string>().Concat(obj.GetSafeValue<ScriptArray>("children").OfType<string>()).ToHashSet().OrderBy(x => x);
                        nsChildren.Clear();
                        nsChildren.AddRange(nsChildrenConcat);
                        
                        nsScriptObject.GetSafeValue<ScriptArray>("assemblies").AddRange(obj.GetSafeValue<ScriptArray>("assemblies"));
                        var langs = nsScriptObject.GetSafeValue<ScriptArray>("langs");
                        foreach (var lang in obj.GetSafeValue<ScriptArray>("langs"))
                        {
                            if (!langs.Contains(lang))
                            {
                                langs.Add(lang);
                            }
                        }
                    }
                    else
                    {
                        if (Cache.Objects.Contains(uid))
                        {
                            Site.Error($"The api dotnet uid {uid} is already registered.");
                        }
                        
                        Cache.Objects[uid] = obj;
                    }
                }
            }

            // Add namespaces when we are done
            Cache.Namespaces.AddRange(namespaces.OrderBy(x => x.Key).Select(x => x.Value));
        }

        FileSystem GetCacheFileSystem()
        {
            return Site.CacheSiteFileSystem.GetOrCreateSubFileSystem((UPath)"/api/dotnet");
        }

        private void CollectProjectsFromConfiguration()
        {
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
        }

        private void BuildProjectsAndGenerateApi()
        {
            var cachefs = GetCacheFileSystem();
            var sharedProperties = Config.Properties ?? new ScriptObject();

            // Clean and build each project
            foreach (var project in Projects)
            {
                var apiFileName = $"{project.Name}.api.json";
                var apiJson = (UPath)$"/{apiFileName}";

                project.CachePath = apiJson;
                var apiFileCached = cachefs.FileExists(apiJson);
                project.CacheState = apiFileCached ? ApitDotNetCacheState.Found : ApitDotNetCacheState.NotFound;
                bool requiresRebuild = !apiFileCached;

                Site.Info(apiFileCached ?
                    $"The api dotnet `{apiFileName}` is already cached. Verifying changes for `{project.Name}`." :
                    $"Start building api dotnet for `{project.Name}`."
                );
                var clock = Stopwatch.StartNew();
                var buildProject = new DotNetProgram("build")
                {
                    Arguments =
                    {
                        $"-c", Config.SolutionConfiguration ?? "Release",
                        project.Path
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
                        if (requiresRebuild)
                        {
                            project.CacheState = ApitDotNetCacheState.Invalid;
                            Site.Error($"Unable to build api dotnet for `{project.Name}`");
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
                        project.CacheState = ApitDotNetCacheState.New;
                    }
                }
                catch (Exception ex)
                {
                    Site.Error($"Error while building api dotnet for `{project.Name}`. Reason: {ex.Message}");
                }
            }
        }

        private bool LoadGeneratedApis()
        {
            if (!Site.Config.SharedCache.TryGetValue(GlobalCacheKey.Instance, out var apiGlobalCacheObj) || !(apiGlobalCacheObj is ScriptObject))
            {
                apiGlobalCacheObj = new ScriptObject();
                Site.Config.SharedCache[GlobalCacheKey.Instance] = apiGlobalCacheObj;
            }

            var apiGlobalCache = (ScriptObject) apiGlobalCacheObj;

            var validAssemblyNames = new HashSet<string>();

            bool rebuildCache = false;
            
            var cacheFs = GetCacheFileSystem();
            foreach (var project in Projects)
            {
                switch (project.CacheState)
                {
                    case ApitDotNetCacheState.Invalid:
                        break;
                    case ApitDotNetCacheState.NotFound:
                        break;
                    case ApitDotNetCacheState.Found:
                        project.Api = apiGlobalCache.GetSafeValue<ScriptObject>(project.Name);
                        if (project.Api == null)
                        {
                            goto case ApitDotNetCacheState.New;
                        }
                        else
                        {
                            validAssemblyNames.Add(project.Name);
                        }
                        break;
                    case ApitDotNetCacheState.New:
                    {
                        var clock = Stopwatch.StartNew();
                        using var stream = cacheFs.OpenFile(project.CachePath, FileMode.Open, FileAccess.Read);
                        project.Api = (ScriptObject) JsonUtil.FromStream(stream, (string) project.CachePath);
                        Site.Info($"Api loaded for {project.Name} in {clock.Elapsed.TotalMilliseconds}ms {project.Api.Count} members");
                        apiGlobalCache[project.Name] = project.Api;
                        validAssemblyNames.Add(project.Name);
                        rebuildCache = true;
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Cleanup the cache if assemblies loaded are no longer necessary
            foreach (var cachedName in apiGlobalCache.Keys.ToList())
            {
                if (!validAssemblyNames.Contains(cachedName))
                {
                    rebuildCache = true;
                    apiGlobalCache.Remove(cachedName);
                }
            }

            return rebuildCache;
        }

        private class GlobalCacheKey
        {
            public static readonly GlobalCacheKey Instance = new GlobalCacheKey();
        }
    }

    public class ApiDotNetCache : ScriptObject
    {
        public ApiDotNetCache()
        {
            Namespaces = new ScriptArray<ScriptObject>();
            Objects = new ScriptObject();
        }

        public new void Clear()
        {
            Namespaces.Clear();
            Objects.Clear();
        }

        public ScriptArray<ScriptObject> Namespaces
        {
            get => this.GetSafeValue<ScriptArray<ScriptObject>>("namespaces");
            private init => this.SetValue("namespaces", value, true);
        }

        public ScriptObject Objects
        {
            get => this.GetSafeValue<ScriptObject>("objects");
            private init => this.SetValue("objects", value, true);
        }
    }
}