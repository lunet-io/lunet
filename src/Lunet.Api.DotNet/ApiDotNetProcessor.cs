// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using DotNet.Globbing;
using Lunet.Api.DotNet.Extractor;
using Lunet.Core;
using Lunet.Json;
using Lunet.Menus;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Scriban.Functions;
using Zio;
using Zio.FileSystems;

namespace Lunet.Api.DotNet;

public class ApiDotNetProcessor : ProcessorBase<ApiDotNetPlugin>
{
    private const string IncludeReferenceAssembliesProperty = "LunetApiDotNetIncludeAssemblies";

    private readonly string _customMsBuildFileProps;
    private readonly Stack<ScriptObjectCollection> _pool;
    private readonly ScriptObject _helpers;
    private readonly string SharedKey = "api-dotnet-object";

    public ApiDotNetProcessor(ApiDotNetPlugin plugin, ApiDotNetConfig config) : base(plugin)
    {
        _pool = new Stack<ScriptObjectCollection>();
        Config = config;
        _customMsBuildFileProps = Path.Combine(AppContext.BaseDirectory, SiteFileSystems.SharedFolderName, SiteFileSystems.LunetFolderName, SiteFileSystems.ModulesFolderName, "api", "dotnet", "Lunet.Api.DotNet.Extractor.props");

        // Cache the result of the ApitDotNetCache in memory
        ApiDotNetObject = (ApiDotNetObject)Site.Config.SharedCache.GetOrAdd(SharedKey, new ApiDotNetObject());
        Projects = new List<ApiDotNetProject>();
        _helpers = new ScriptObject();

        Site.Builtins.SetValue("apiref", DelegateCustomFunction.CreateFunc((Func<string, ScriptObject?>)ApiRef), true);
    }

    public ApiDotNetConfig Config { get; }
        
    public List<ApiDotNetProject> Projects { get; }
        
    public ApiDotNetObject ApiDotNetObject { get; }

    private ScriptObject? ApiRef(string arg)
    {
        if (ApiDotNetObject.Objects.TryGetValue(arg, out var value))
        {
            return value as ScriptObject;
        }

        return null;
    }

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
        var apiBasePath = NormalizeApiBasePath(Config.BasePath);
        var pagesByUid = new Dictionary<string, DynamicContentObject>(StringComparer.Ordinal);

        // Register all xref
        foreach (var refKeyPair in ApiDotNetObject.References)
        {
            var uid = refKeyPair.Key;
            var obj = (ScriptObject)refKeyPair.Value;
            Site.Content.Finder.RegisterExtraContent(new ExtraContent()
                {
                    Uid = uid,
                    DefinitionUid = obj.GetSafeValue<string>("definition"),
                    Name = obj.GetSafeValue<string>("name"),
                    FullName = obj.GetSafeValue<string>("fullName"),
                    IsExternal = obj.GetSafeValue<bool>("isExternal")
                }
            );
        }

        var objects = ApiDotNetObject.Objects;
        foreach (var objPair in objects)
        {
            var obj = (ScriptObject)objPair.Value;
            var uid = obj.GetSafeValue<string>("uid");
            if (string.IsNullOrWhiteSpace(uid))
            {
                continue;
            }

            var url = $"{apiBasePath}/{UidHelper.Handleize(uid)}/readme.md";

            DynamicContentObject? content = null;
            switch (GetTypeFromModel(obj))
            {
                case "Namespace":
                {
                    content = new DynamicContentObject(Site, url, "api", url)
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
                    content = new DynamicContentObject(Site, url, "api", url)
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
                var xrefName = obj["name"] as string;
                var xrefFullName = obj["fullName"] as string ?? xrefName;
                content[PageVariables.XRefName] = xrefName;
                content[PageVariables.XRefFullName] = xrefFullName;
                content.Title = $"{xrefName} {obj["type"]}";

                // Copy helpers as if it was part of the file
                _helpers.CopyTo(content.ScriptObjectLocal);
                content.Initialize();
                Site.DynamicPages.Add(content);
                pagesByUid[uid] = content;
            }
        }

        // Create the root page
        DynamicContentObject apiRootPage;
        {
            var path = $"{apiBasePath}/readme.md";
            var content = new DynamicContentObject(Site, path, "api", path)
            {
                ScriptObjectLocal = new ScriptObject(), // only used to let layout processor running
                LayoutType = "api-dotnet",
            };

            content.Uid = "api-dotnet";
            content.Layout = Config.Layout ?? "_default";
            content.ContentType = ContentType.Markdown;
            content.Title = Config.Title ?? $"{Site.GetSafeValue<string>("title")} .NET API Reference";
            content.ScriptObjectLocal.SetValue("api", ApiDotNetObject, true);
            content["notoc"] = true;

            // Copy helpers as if it was part of the file
            _helpers.CopyTo(content.ScriptObjectLocal);
            content.Initialize();
            Site.DynamicPages.Add(content);
            apiRootPage = content;
        }

        ConfigureGeneratedMenu(apiRootPage, pagesByUid);
    }

    private static string NormalizeApiBasePath(string? configuredPath)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(configuredPath)
            ? "/api"
            : configuredPath.Trim().Replace('\\', '/');

        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            normalizedPath = "/" + normalizedPath;
        }

        normalizedPath = normalizedPath.TrimEnd('/');
        return string.IsNullOrWhiteSpace(normalizedPath) ? "/api" : normalizedPath;
    }

    private void ConfigureGeneratedMenu(DynamicContentObject apiRootPage, IReadOnlyDictionary<string, DynamicContentObject> pagesByUid)
    {
        var menuPlugin = Plugin.Menus;
        if (menuPlugin is null)
        {
            return;
        }

        var menuName = string.IsNullOrWhiteSpace(Config.MenuName) ? "api" : Config.MenuName;
        var menuTitle = string.IsNullOrWhiteSpace(Config.MenuTitle)
            ? Config.Title ?? $"{Site.GetSafeValue<string>("title")} .NET API Reference"
            : Config.MenuTitle;

        var rootMenu = new MenuObject
        {
            Name = menuName,
            Title = menuTitle,
            Path = (string)apiRootPage.Path,
            Page = apiRootPage,
            Folder = ApiDotNetObject.Namespaces.Count > 0,
            Generated = true,
            Pre = GetMenuIconMarkup("Api"),
            Width = Config.MenuWidth,
        };

        menuPlugin.RegisterMenu(menuName, rootMenu, overwrite: true);
        menuPlugin.SetPageMenu(apiRootPage, rootMenu, force: true);

        var recursionGuard = new HashSet<string>(StringComparer.Ordinal);
        foreach (var namespaceObject in ApiDotNetObject.Namespaces
                     .OrderBy(x => x.GetSafeValue<string>("name"), StringComparer.OrdinalIgnoreCase))
        {
            var namespaceMenu = CreateGeneratedMenuItem(rootMenu, namespaceObject, pagesByUid, menuPlugin);
            if (namespaceMenu is null)
            {
                continue;
            }

            BuildGeneratedMenuChildren(namespaceMenu, namespaceObject, pagesByUid, recursionGuard, menuPlugin);
        }
    }

    private void BuildGeneratedMenuChildren(
        MenuObject parentMenu,
        ScriptObject parentObject,
        IReadOnlyDictionary<string, DynamicContentObject> pagesByUid,
        HashSet<string> recursionGuard,
        MenuPlugin menuPlugin)
    {
        var parentUid = parentObject.GetSafeValue<string>("uid");
        if (string.IsNullOrWhiteSpace(parentUid) || !recursionGuard.Add(parentUid))
        {
            return;
        }

        try
        {
            var childEntries = CollectChildEntries(parentObject, pagesByUid);
            if (childEntries.Count == 0)
            {
                parentMenu.Folder = false;
                return;
            }

            if (IsTypeDeclarationKind(GetTypeFromModel(parentObject)))
            {
                BuildGeneratedTypeMemberGroups(parentMenu, childEntries, pagesByUid, recursionGuard, menuPlugin);
                return;
            }

            parentMenu.Folder = true;
            foreach (var childObject in childEntries
                         .OrderBy(x => GetMenuSortOrder(GetTypeFromModel(x)))
                         .ThenBy(x => x.GetSafeValue<string>("name"), StringComparer.OrdinalIgnoreCase))
            {
                var childMenu = CreateGeneratedMenuItem(parentMenu, childObject, pagesByUid, menuPlugin);
                if (childMenu is null)
                {
                    continue;
                }

                BuildGeneratedMenuChildren(childMenu, childObject, pagesByUid, recursionGuard, menuPlugin);
            }
        }
        finally
        {
            recursionGuard.Remove(parentUid);
        }
    }

    private List<ScriptObject> CollectChildEntries(ScriptObject parentObject, IReadOnlyDictionary<string, DynamicContentObject> pagesByUid)
    {
        var children = parentObject.GetSafeValue<ScriptArray>("children");
        if (children is null || children.Count == 0)
        {
            return [];
        }

        var childEntries = new List<ScriptObject>();
        foreach (var childUid in children.OfType<string>().Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(childUid))
            {
                continue;
            }

            if (ApiDotNetObject.Objects.GetSafeValue<ScriptObject>(childUid) is not ScriptObject childObject)
            {
                continue;
            }

            if (!pagesByUid.ContainsKey(childUid))
            {
                continue;
            }

            childEntries.Add(childObject);
        }

        return childEntries;
    }

    private void BuildGeneratedTypeMemberGroups(
        MenuObject parentMenu,
        IReadOnlyList<ScriptObject> childEntries,
        IReadOnlyDictionary<string, DynamicContentObject> pagesByUid,
        HashSet<string> recursionGuard,
        MenuPlugin menuPlugin)
    {
        var childrenByKind = childEntries
            .Select(child => (Child: child, Kind: GetTypeFromModel(child)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Kind))
            .GroupBy(item => item.Kind!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Child).ToList(), StringComparer.Ordinal);

        parentMenu.Folder = childrenByKind.Count > 0;

        foreach (var (kind, title) in TypeMemberMenuGroups)
        {
            if (!childrenByKind.TryGetValue(kind, out var members) || members.Count == 0)
            {
                continue;
            }

            var groupMenu = new MenuObject
            {
                Parent = parentMenu,
                Title = title,
                Folder = true,
                Generated = true,
                Pre = GetMenuIconMarkup(kind),
                Url = GetGeneratedTypeMemberGroupUrl(parentMenu, title),
            };
            parentMenu.Children.Add(groupMenu);

            foreach (var child in members.OrderBy(x => x.GetSafeValue<string>("name"), StringComparer.OrdinalIgnoreCase))
            {
                var childMenu = CreateGeneratedMenuItem(groupMenu, child, pagesByUid, menuPlugin);
                if (childMenu is null)
                {
                    continue;
                }

                BuildGeneratedMenuChildren(childMenu, child, pagesByUid, recursionGuard, menuPlugin);
            }
        }
    }

    private MenuObject? CreateGeneratedMenuItem(
        MenuObject parentMenu,
        ScriptObject objectModel,
        IReadOnlyDictionary<string, DynamicContentObject> pagesByUid,
        MenuPlugin menuPlugin)
    {
        var uid = objectModel.GetSafeValue<string>("uid");
        if (string.IsNullOrWhiteSpace(uid) || !pagesByUid.TryGetValue(uid, out var page))
        {
            return null;
        }

        var title = page.GetSafeValue<string>(PageVariables.XRefName)
                    ?? objectModel.GetSafeValue<string>("name")
                    ?? page.Title
                    ?? uid;

        var pagePath = page.Path.IsNull ? (page.UrlWithoutBasePath ?? page.Url) : (string)page.Path;
        if (string.IsNullOrWhiteSpace(pagePath))
        {
            return null;
        }

        var menuObject = new MenuObject
        {
            Parent = parentMenu,
            Path = pagePath,
            Page = page,
            Title = title,
            Generated = true,
            Pre = GetMenuIconMarkup(GetTypeFromModel(objectModel)),
        };

        parentMenu.Children.Add(menuObject);
        menuPlugin.SetPageMenu(page, menuObject, force: true);
        return menuObject;
    }

    private static int GetMenuSortOrder(string? kind)
    {
        return kind switch
        {
            "Api" => -1,
            "Namespace" => 0,
            "Class" => 10,
            "Struct" => 11,
            "Interface" => 12,
            "Enum" => 13,
            "Delegate" => 14,
            "Constructor" => 20,
            "Field" => 21,
            "Property" => 22,
            "Method" => 23,
            "Event" => 24,
            "Operator" => 25,
            "Extension" => 26,
            "EiiMethod" => 27,
            _ => 100,
        };
    }

    private static bool IsTypeDeclarationKind(string? kind)
    {
        return kind is "Class" or "Struct" or "Interface" or "Enum" or "Delegate";
    }

    private static string? GetGeneratedTypeMemberGroupUrl(MenuObject parentMenu, string title)
    {
        var pageUrl = parentMenu.Page?.Url;
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            return null;
        }

        var anchor = StringFunctions.Handleize(title);
        return string.IsNullOrWhiteSpace(anchor) ? pageUrl : $"{pageUrl}#{anchor}";
    }

    private static string? GetMenuIconMarkup(string? kind)
    {
        var icon = kind switch
        {
            "Api" => "bi-braces-asterisk",
            "Namespace" => "bi-diagram-3",
            "Class" => "bi-box",
            "Struct" => "bi-boxes",
            "Interface" => "bi-diagram-3",
            "Enum" => "bi-list-ul",
            "Delegate" => "bi-code-slash",
            "Constructor" => "bi-hammer",
            "Field" => "bi-hash",
            "Property" => "bi-sliders",
            "Method" => "bi-gear",
            "Event" => "bi-bell",
            "Operator" => "bi-calculator",
            "Extension" => "bi-plugin",
            "EiiMethod" => "bi-link-45deg",
            _ => null,
        };

        return icon is null ? null : $"<i class='bi {icon}' aria-hidden='true'></i> ";
    }

    private static readonly (string Kind, string Title)[] TypeMemberMenuGroups =
    [
        ("Class", "Nested Classes"),
        ("Struct", "Nested Structs"),
        ("Interface", "Nested Interfaces"),
        ("Enum", "Nested Enums"),
        ("Delegate", "Nested Delegates"),
        ("Constructor", "Constructors"),
        ("Field", "Fields"),
        ("Property", "Properties"),
        ("Method", "Methods"),
        ("Event", "Events"),
        ("Operator", "Operators"),
        ("Extension", "Extensions"),
        ("EiiMethod", "Explicit Interface Implementation Methods"),
    ];

    private void UpdateUid()
    {
        // Make sure the cache is cleared before starting again
        ApiDotNetObject.Clear();

        var namespaces = new Dictionary<string, ScriptObject>();
            
        foreach (var project in Projects)
        {
            if (project.Api == null) continue;

            var objects = project.Api.GetSafeValue<ScriptArray>("items");

            foreach (var entryItemsAndReferences in objects.OfType<ScriptObject>())
            {
                foreach (var obj in ((ScriptArray) entryItemsAndReferences["items"]).OfType<ScriptObject>())
                {
                    ProcessItem(obj, namespaces);
                }

                foreach (var obj in ((ScriptArray)entryItemsAndReferences["references"]).OfType<ScriptObject>())
                {
                    var uid = (string)obj["uid"];
                    if (!ApiDotNetObject.References.ContainsKey(uid))
                    {
                        ApiDotNetObject.References[uid] = obj;
                    }
                }
            }
        }

        // Add namespaces when we are done
        ApiDotNetObject.Namespaces.AddRange(namespaces.OrderBy(x => x.Key).Select(x => x.Value));
    }

    private void ProcessItem(ScriptObject obj, Dictionary<string, ScriptObject> namespaces)
    {
        var uid = (string) obj["uid"];
        if (obj.GetSafeValue<string>("type") == "Namespace")
        {
            ScriptObject nsScriptObject;
            if (!namespaces.TryGetValue(uid, out var nsObject))
            {
                nsObject = new ScriptObject();
                nsScriptObject = (ScriptObject) nsObject;
                namespaces.Add(uid, nsScriptObject);
                nsScriptObject.Add("uid", uid);
                nsScriptObject.Add("commentId", obj["commentId"]);
                nsScriptObject.Add("id", obj["id"]);
                nsScriptObject.Add("name", obj["name"]);
                nsScriptObject.Add("nameWithType", obj["nameWithType"]);
                nsScriptObject.Add("fullName", obj["fullName"]);
                nsScriptObject.Add("summary", obj["summary"]);
                nsScriptObject.Add("remarks", obj["remarks"]);
                nsScriptObject.Add("example", obj["example"]);
                nsScriptObject.Add("type", obj["type"]);
                nsScriptObject.Add("children", new ScriptArray());
                nsScriptObject.Add("assemblies", new ScriptArray());
                nsScriptObject.Add("langs", new ScriptArray());
                // TODO: merge summary/remarks/example...
                ApiDotNetObject.Objects[uid] = nsObject;
            }
            else
            {
                nsScriptObject = (ScriptObject) nsObject;
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
                if (!objects.TryGetValue(childId, out var childObjObject) || childObjObject is not ScriptObject childObj)
                {
                    continue;
                }

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

    private static string? GetTypeFromModel(ScriptObject obj)
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
        var globalIncludedReferences = ParseReferenceAssemblies(Config.References, "api.dotnet.references");
        var projects = Config.Projects;
        if (projects is null)
        {
            return;
        }

        // Collect csproj
        var rootDirectory = Site.SiteFileSystem.ConvertPathToInternal(UPath.Root);
        foreach (var projectEntry in projects)
        {
            string? projectName = null;
            var projectPath = projectEntry as string;
            ScriptObject? projectProperties = null;
            List<string>? projectIncludedReferences = null;

            // TODO: log error
            if (projectPath == null)
            {
                var projectObject = projectEntry as ScriptObject;
                if (projectObject != null)
                {
                    projectName = projectObject["name"] as string;
                    projectPath = projectObject["path"] as string;
                    projectProperties = projectObject["properties"] as ScriptObject;
                    projectIncludedReferences = ParseReferenceAssemblies(projectObject["references"], $"api.dotnet.projects[{projectName ?? projectPath}].references");
                }
            }

            if (projectPath == null)
            {
                Site.Error($"Invalid project description {projectEntry}. Expecting a string or an object with at least a path {{ path: '...' }}");
                continue;
            }

            var includedReferences = MergeIncludedReferenceAssemblies(globalIncludedReferences, projectIncludedReferences);

            var entryPath = Path.GetFullPath(Path.Combine(rootDirectory, projectPath));
            if (File.Exists(entryPath))
            {
                Projects.Add(new ApiDotNetProject()
                {
                    Name = projectName ?? Path.GetFileNameWithoutExtension(entryPath),
                    Path = entryPath,
                    Properties = projectProperties ?? new ScriptObject(),
                    IncludedReferenceAssemblies = includedReferences,
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
                            Properties = projectProperties ?? new ScriptObject(),
                            IncludedReferenceAssemblies = includedReferences,
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
                WorkingDirectory = Path.GetDirectoryName(project.Path) ?? Environment.CurrentDirectory
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
            if (project.IncludedReferenceAssemblies.Count > 0)
            {
                buildProject.Properties[IncludeReferenceAssembliesProperty] = string.Join(";", project.IncludedReferenceAssemblies);
            }

            try
            {
                var resultAsText = buildProject.Run();
                //Site.Info($"Result: {resultAsText}");
                var results = ExtractorHelper.FindResults(resultAsText);
                buildProject.Properties.TryGetValue("TargetFramework", out var targetFrameworkObject);
                var targetFramework = targetFrameworkObject as string ?? targetFrameworkObject?.ToString();
                var selectedResult = ApiDotNetResultSelector.SelectBestResult(results, project.Path, project.Name, targetFramework ?? string.Empty);

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
                    if (string.IsNullOrEmpty(selectedResult))
                    {
                        project.CacheState = ApitDotNetCacheState.Invalid;
                        Site.Error($"Unable to select the api dotnet output for `{project.Name}`. Reported outputs: {string.Join(", ", results)}");
                        continue;
                    }

                    if (results.Count > 1)
                    {
                        Site.Warning($"Multiple api dotnet output generated for `{project.Name}`. Using selected output {selectedResult}");
                    }

                    var file = selectedResult;
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

    private List<string> ParseReferenceAssemblies(object? referencesObject, string contextName)
    {
        if (referencesObject == null)
        {
            return new List<string>();
        }

        if (referencesObject is string singleReference)
        {
            singleReference = singleReference.Trim();
            if (singleReference.Length == 0)
            {
                return new List<string>();
            }

            return new List<string> { singleReference };
        }

        if (referencesObject is not ScriptArray referencesArray)
        {
            Site.Error($"Invalid `{contextName}` value. Expecting a string or an array of strings.");
            return new List<string>();
        }

        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var referenceObject in referencesArray)
        {
            if (referenceObject is not string reference)
            {
                Site.Error($"Invalid `{contextName}` entry `{referenceObject}`. Expecting only strings.");
                continue;
            }

            reference = reference.Trim();
            if (reference.Length == 0)
            {
                continue;
            }

            references.Add(reference);
        }

        return references.ToList();
    }

    private static List<string> MergeIncludedReferenceAssemblies(List<string>? globalReferences, List<string>? projectReferences)
    {
        if ((globalReferences == null || globalReferences.Count == 0) && (projectReferences == null || projectReferences.Count == 0))
        {
            return new List<string>();
        }

        var mergedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (globalReferences != null)
        {
            foreach (var reference in globalReferences)
            {
                mergedReferences.Add(reference);
            }
        }

        if (projectReferences != null)
        {
            foreach (var reference in projectReferences)
            {
                mergedReferences.Add(reference);
            }
        }

        return mergedReferences.ToList();
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
                    project.Api = JsonUtil.FromStream(stream, (string) project.CachePath) as ScriptObject ?? new ScriptObject();
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
