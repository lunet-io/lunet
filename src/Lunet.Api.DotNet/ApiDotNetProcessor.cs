// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using DotNet.Globbing;
using Lunet.Api.DotNet.Extractor;
using Lunet.Core;
using Lunet.Json;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Zio;
using Zio.FileSystems;

namespace Lunet.Api.DotNet
{
    public class ApiDotNetProcessor : ProcessorBase<ApiDotNetPlugin>
    {
        private readonly string _customMsBuildFileProps;
        private readonly Stack<ScriptObjectCollection> _pool;
        private readonly ScriptObject _helpers;

        public ApiDotNetProcessor(ApiDotNetPlugin plugin, ApiDotNetConfig config) : base(plugin)
        {
            _pool = new Stack<ScriptObjectCollection>();
            Config = config;
            _customMsBuildFileProps = Path.Combine(AppContext.BaseDirectory, SiteFileSystems.SharedFolderName, SiteFileSystems.LunetFolderName, SiteFileSystems.ModulesFolderName, "api", "dotnet", "Lunet.Api.DotNet.Extractor.props");
            ApiDotNetObject = new ApiDotNetObject(this);
            Projects = new List<ApiDotNetProject>();
            _helpers = new ScriptObject();
        }

        public ApiDotNetConfig Config { get; }
        
        public List<ApiDotNetProject> Projects { get; }
        
        public ApiDotNetObject ApiDotNetObject { get; }

        public override void Process(ProcessingStage stage)
        {
            if (!File.Exists(_customMsBuildFileProps))
            {
                Site.Error($"Invalid api dotnet setup. The file {_customMsBuildFileProps} was not found");
                return;
            }

            if (Config.Projects == null || Config.Projects.Count == 0)
            {
                Site.Warning("No projects or assemblies configured for api.dotnet");
                return;
            }

            // load include helpers
            var includeHelperPath = Config.IncludeHelper ?? "_builtins/api-dotnet-helpers.sbn-html";
            if (!Site.Scripts.TryImportInclude(includeHelperPath, _helpers))
            {
                Site.Error($"Unable to load include helper `{includeHelperPath}`");
                return;
            }

            CollectProjectsFromConfiguration();
            
            BuildProjectsAndGenerateApi();

            if (LoadGeneratedApis())
            {
                UpdateUid();

                ProcessObjects();
            }

            GeneratePages();
        }

        private void GeneratePages()
        {
            var objects = ApiDotNetObject.Objects;
            foreach (var objPair in objects)
            {
                var obj = (ScriptObject)objPair.Value;
                var uid = obj.GetSafeValue<string>("uid");
                var url = $"/api/{uid}/readme.md";

                DynamicContentObject content = null;
                switch (GetTypeFromModel(obj))
                {
                    case "Namespace":
                    {
                        content = new DynamicContentObject(Site, url, "api")
                        {
                            ScriptObjectLocal = new ScriptObject(), // only used to let layout processor running
                            LayoutType = "api-dotnet-namespace",
                        };

                        content.ScriptObjectLocal["namespace"] = obj;
                        break;
                    }
                    case "Class":
                    case "Interface":
                    case "Struct":
                    case "Enum":
                    case "Delegate":
                    case "Constructor":
                    case "Field":
                    case "Property":
                    case "Method":
                    case "Operator":
                    case "Event":
                    case "Extension":
                    case "EiiMethod":
                    {
                            content = new DynamicContentObject(Site, url, "api")
                        {
                            ScriptObjectLocal = new ScriptObject(), // only used to let layout processor running
                            LayoutType = "api-dotnet-member",
                        };

                        content.ScriptObjectLocal["member"] = obj;
                        break;
                    }
                }

                if (content != null)
                {
                    content.Uid = uid;
                    content.Layout = Config.Layout ?? "_default";
                    content.ContentType = ContentType.Markdown;
                    content.Title = $"{obj["name"]} {obj["type"]}";

                    // Copy helpers as if it was part of the file
                    _helpers.CopyTo(content.ScriptObjectLocal);
                    content.Initialize();
                    Site.Content.Finder.RegisterUid(content);
                    Site.DynamicPages.Add(content);
                }
            }
        }

        private void UpdateUid()
        {
            // Make sure the cache is cleared before starting again
            ApiDotNetObject.Clear();

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
                        if (!namespaces.TryGetValue(uid, out var nsObject))
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
                            // TODO: merge summary/remarks/example...
                            ApiDotNetObject.Objects[uid] = nsObject;
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
                        if (ApiDotNetObject.Objects.Contains(uid))
                        {
                            Site.Error($"The api dotnet uid {uid} is already registered.");
                        }
                        
                        ApiDotNetObject.Objects[uid] = obj;
                    }
                }
            }

            // Add namespaces when we are done
            ApiDotNetObject.Namespaces.AddRange(namespaces.OrderBy(x => x.Key).Select(x => x.Value));
        }

        private void ProcessObjects()
        {
            var extensionMethods = new List<ScriptObject>();
            var objects = ApiDotNetObject.Objects;
            foreach (var objPair in objects)
            {
                var obj = (ScriptObject)objPair.Value;

                var commonMembers = GetTypeFromModel(obj) == "Namespace" ? CommonNamespaceMembers : CommonMembers;
                foreach (var commonMember in commonMembers)
                {
                    RecycleCollection(obj, commonMember.name);
                }

                var ids = obj.GetSafeValue<ScriptArray>("children")?.OfType<string>();
                if (ids == null) continue;

                foreach (var childId in ids)
                {
                    var childObj = (ScriptObject)objects[childId];
                    if (childObj.GetSafeValue<bool>("isExtensionMethod"))
                    {
                        extensionMethods.Add(childObj);
                    }

                    var typeFromModel = GetTypeFromModel(childObj);
                    foreach (var commonMember in commonMembers)
                    {
                        if (commonMember.type != typeFromModel) continue;
                        AddToMemberCollection(obj, commonMember.name, childObj);
                    }
                }
            }

            // Attach extension methods to the this type if possible.
            foreach (var extension in extensionMethods)
            {
                var thisTypeUid = (string)((ScriptObject) extension.GetSafeValue<ScriptObject>("syntax").GetSafeValue<ScriptArray>("parameters")[0])["type"];
                // If the type is a generic, we don't support them for now.
                if (thisTypeUid.StartsWith("{")) continue;

                // Try to resolve the this type of the extension method
                if (objects.TryGetValue(thisTypeUid, out var thisTypeObj) && thisTypeObj is ScriptObject thisType)
                {
                    AddToMemberCollection(thisType, "extensions", extension);
                }
            }

            // TODO: order extension methods by name
        }

        private static string GetTypeFromModel(ScriptObject obj)
        {
            var type = obj["type"] as string;
            if (type == "Method" && obj.GetSafeValue<bool>("isEii"))
            {
                type = "EiiMethod";
            }

            return type;
        }

        private readonly (string name, string type)[] CommonMembers = new (string name, string type)[]
        {
            ("constructors", "Constructor"),
            ("fields", "Field"),
            ("properties", "Property"),
            ("methods", "Method"),
            ("events", "Event"),
            ("operators", "Operator"),
            ("extensions", "Extension"),
            ("explicit_interface_implementation_methods", "EiiMethod"),
        };

        private readonly (string name, string type)[] CommonNamespaceMembers = new (string name, string type)[]
        {
            ("classes", "Class"),
            ("structs", "Struct"),
            ("interfaces", "Interface"),
            ("enums", "Enum"),
            ("delegates", "Delegate"),
        };

        private void RecycleCollection(ScriptObject obj, string name)
        {
            if (obj[name] is ScriptObjectCollection collect)
            {
                collect.Clear();
                _pool.Push(collect);
            }
            obj.Remove(name);
        }

        private void AddToMemberCollection(ScriptObject obj, string memberName, ScriptObject member)
        {
            if (!obj.TryGetValue(memberName, out var collectObj) || collectObj is not ScriptObjectCollection)
            {
                collectObj = _pool.Count > 0 ? _pool.Pop() : new ScriptObjectCollection();
                obj[memberName] = collectObj;
            }

            var collection = (ScriptObjectCollection) collectObj;
            collection.Add(member);
        }

        private class ScriptObjectCollection : List<ScriptObject>
        {
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

                var rerunArguments = new List<string>();

                rebuild_doc:

                var buildProject = new DotNetProgram("build")
                {
                    Arguments =
                    {
                        $"-c", Config.SolutionConfiguration ?? "Release",
                        project.Path
                    },
                    WorkingDirectory = Path.GetDirectoryName(project.Path)
                };
                buildProject.Arguments.AddRange(rerunArguments);

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
                            if (rerunArguments.Count > 0)
                            {
                                project.CacheState = ApitDotNetCacheState.Invalid;
                                Site.Error($"Unable to build api dotnet for `{project.Name}`");
                            }
                            else
                            {
                                rerunArguments.Add("-t:Clean;Build");
                                goto rebuild_doc;
                            }
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
                    Site.Error(ex, $"Error while building api dotnet for `{project.Name}`. Reason: {ex.Message}");
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
}