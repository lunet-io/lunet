// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Scripts;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Extends;

public class ExtendsModule : SiteModule<ExtendsPlugin>
{
}


/// <summary>
/// Manages themes.
/// </summary>
public sealed class ExtendsPlugin : SitePlugin
{
    private const string ExtendsFolderName = "extends";
    private const string GitHubFolderName = "github";
    private const string DefaultGitHubBranch = "main";
    private const string DefaultContentDirectory = "dist";
    private static readonly object DownloadedMainRefsCacheKey = new object();

    private delegate object? ExtendFunctionDelegate(object? o);

    public ExtendsPlugin(SiteObject site) : base(site)
    {
        ExtendsFolder = UPath.Root / ExtendsFolderName;
        PrivateExtendsFolder = UPath.Root / ExtendsFolderName;
        CurrentList = new List<ExtendObject>();
        Site.Builtins.SetValue(SiteVariables.Extends, CurrentList.AsReadOnly(), true);
        Site.Builtins.Import(SiteVariables.ExtendFunction, (ExtendFunctionDelegate)ExtendFunction);
    }

    public UPath ExtendsFolder { get; }

    public UPath PrivateExtendsFolder { get; }

    /// <summary>
    /// Gets the list of themes currently used.
    /// </summary>
    public List<ExtendObject> CurrentList { get; }

    public ExtendObject? LoadExtend(string extendName, bool isPrivate)
    {
        if (extendName == null) throw new ArgumentNullException(nameof(extendName));
        var request = ParseQuery(extendName, isPrivate);
        return LoadExtend(request);
    }

    public ExtendObject? TryInstall(string extendName, bool isPrivate = false)
    {
        if (extendName == null) throw new ArgumentNullException(nameof(extendName));
        var request = ParseQuery(extendName, isPrivate);
        return TryInstall(request);
    }

    private ExtendObject? LoadExtend(ExtendRequest request)
    {
        ExtendObject? extendObject = null;

        foreach (var existingExtend in CurrentList)
        {
            if (existingExtend.FullName == request.FullName)
            {
                extendObject = existingExtend;
                break;
            }
        }

        if (extendObject == null)
        {
            extendObject = TryInstall(request);
            if (extendObject == null)
            {
                return null;
            }
            CurrentList.Add(extendObject);

            var configPath = new FileEntry(extendObject.FileSystem, UPath.Root / SiteFileSystems.DefaultConfigFileName);
            object? result;
            Site.Scripts.TryImportScriptFromFile(configPath, Site, ScriptFlags.AllowSiteFunctions, out result);
        }

        // Register the extensions as a content FileSystem
        Site.AddContentFileSystem(extendObject.FileSystem);

        if (Site.CanTrace())
        {
            Site.Trace($"Using extension/theme `{request.FullName}` from `{extendObject.FileSystem.ConvertPathToInternal(UPath.Root)}`");
        }

        return extendObject;
    }

    private ExtendObject? TryInstall(ExtendRequest request)
    {
        var installPath = GetInstallPath(request);
        var localMetaPath = SiteFileSystems.LunetFolder / installPath;
        var localExtendDir = new DirectoryEntry(Site.SiteFileSystem, localMetaPath);
        var cacheExtendDir = new DirectoryEntry(Site.CacheMetaFileSystem, installPath);
        IFileSystem? extendFileSystem = null;

        bool isLatestMainRequest = request.IsGitHub && string.IsNullOrEmpty(request.Tag);
        bool shouldRefreshLatestMain = isLatestMainRequest && ShouldRefreshLatestMain(request.CacheKey);

        if (!shouldRefreshLatestMain)
        {
            if (cacheExtendDir.Exists)
            {
                extendFileSystem = new SubFileSystem(cacheExtendDir.FileSystem, cacheExtendDir.Path);
            }
            else if (localExtendDir.Exists)
            {
                extendFileSystem = new SubFileSystem(localExtendDir.FileSystem, localExtendDir.Path);
            }
        }

        if (extendFileSystem != null)
        {
            return new ExtendObject(Site, request.FullName, request.Name, request.Tag, null, request.Url, extendFileSystem);
        }

        if (!request.IsGitHub)
        {
            Site.Error($"Unable to find local extension/theme [{request.Name}] from [{localExtendDir}] or [{cacheExtendDir}]. To load from GitHub, use extend \"owner/repo\" or extend {{ repo: \"owner/repo\", tag: \"v1.2.3\" }}.");
            return null;
        }

        if (localExtendDir.Exists && !request.IsPrivate)
        {
            localExtendDir.Delete(true);
        }

        if (cacheExtendDir.Exists && request.IsPrivate)
        {
            cacheExtendDir.Delete(true);
        }

        var destinationDirectory = request.IsPrivate
            ? cacheExtendDir.FileSystem.GetOrCreateSubFileSystem(cacheExtendDir.Path)
            : localExtendDir.FileSystem.GetOrCreateSubFileSystem(localExtendDir.Path);

        var outputDirectory = new DirectoryEntry(destinationDirectory, UPath.Root);
        if (!TryInstallFromGitHub(request, outputDirectory))
        {
            return null;
        }

        if (isLatestMainRequest)
        {
            MarkLatestMainRefreshed(request.CacheKey);
        }

        return new ExtendObject(Site, request.FullName, request.Name, request.Tag, null, request.Url, destinationDirectory);
    }

    private bool TryInstallFromGitHub(ExtendRequest request, DirectoryEntry outputDirectory)
    {
        var zipUrl = request.Tag != null
            ? $"https://github.com/{request.Repository}/archive/refs/tags/{Uri.EscapeDataString(request.Tag)}.zip"
            : $"https://github.com/{request.Repository}/archive/refs/heads/{DefaultGitHubBranch}.zip";

        try
        {
            if (Site.CanInfo())
            {
                Site.Info($"Downloading and installing extension/theme `{request.FullName}` to `{outputDirectory}`");
            }

            using var client = new HttpClient();
            using var stream = client.GetStreamAsync(zipUrl).Result;
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

            if (!TryGetFilterPath(zip, request.Directory, out var filterPath))
            {
                Site.Error($"Unable to find the directory `{request.Directory}` in [{zipUrl}]");
                return false;
            }

            zip.ExtractToDirectory(outputDirectory, filterPath);
            return true;
        }
        catch (Exception ex)
        {
            Site.Error(ex, $"Unable to load extension/theme from Url [{zipUrl}]. Reason:{ex.GetReason()}");
            return false;
        }
    }

    private static bool TryGetFilterPath(ZipArchive zip, string directory, out string? filterPath)
    {
        filterPath = null;
        directory = NormalizeDirectory(directory);

        foreach (var entry in zip.Entries)
        {
            var entryPath = entry.FullName.Replace('\\', '/');
            if (entryPath.Length == 0)
            {
                continue;
            }

            var firstSlash = entryPath.IndexOf('/');
            if (firstSlash <= 0)
            {
                continue;
            }

            var rootDirectory = entryPath.Substring(0, firstSlash);
            var localFilterPath = $"{rootDirectory}/{directory}";
            if (entryPath.Equals(localFilterPath, StringComparison.OrdinalIgnoreCase) ||
                entryPath.StartsWith($"{localFilterPath}/", StringComparison.OrdinalIgnoreCase))
            {
                filterPath = localFilterPath;
                return true;
            }
        }

        return false;
    }

    private static string NormalizeDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return DefaultContentDirectory;
        }

        return directory.Replace('\\', '/').Trim('/');
    }

    private UPath GetInstallPath(ExtendRequest request)
    {
        if (!request.IsGitHub)
        {
            return ExtendsFolder / request.Name;
        }

        return ExtendsFolder
            / GitHubFolderName
            / SanitizePathSegment(request.Owner ?? string.Empty)
            / SanitizePathSegment(request.RepositoryName ?? string.Empty)
            / SanitizePathSegment(request.Tag ?? DefaultGitHubBranch)
            / SanitizePathSegment(NormalizeDirectory(request.Directory));
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "default";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private bool ShouldRefreshLatestMain(string cacheKey)
    {
        var refreshedRefs = GetOrCreateRefreshedMainRefs();
        lock (refreshedRefs)
        {
            return !refreshedRefs.Contains(cacheKey);
        }
    }

    private void MarkLatestMainRefreshed(string cacheKey)
    {
        var refreshedRefs = GetOrCreateRefreshedMainRefs();
        lock (refreshedRefs)
        {
            refreshedRefs.Add(cacheKey);
        }
    }

    private HashSet<string> GetOrCreateRefreshedMainRefs()
    {
        if (Site.Config.SharedCache.TryGetValue(DownloadedMainRefsCacheKey, out var refs) && refs is HashSet<string> hashSet)
        {
            return hashSet;
        }

        hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Site.Config.SharedCache[DownloadedMainRefsCacheKey] = hashSet;
        return hashSet;
    }

    private ExtendRequest ParseQuery(object? query)
    {
        if (query is string queryAsString)
        {
            return ParseQuery(queryAsString, isPrivate: true);
        }

        if (query is not ScriptObject extendObj)
        {
            throw new LunetException($"Unsupported extension/theme query [{query}]. Supports either a string or an object like {{ repo: \"owner/repo\", tag: \"v1.2.3\", directory: \"dist\" }}.");
        }

        var isPrivate = !extendObj.GetSafeValue<bool>("public");
        var name = NormalizeString(extendObj.GetSafeValue<string>("name"));
        var repositoryQuery = NormalizeString(extendObj.GetSafeValue<string>("repo")) ?? NormalizeString(extendObj.GetSafeValue<string>("url"));
        var tag = NormalizeString(extendObj.GetSafeValue<string>("tag")) ?? NormalizeString(extendObj.GetSafeValue<string>("version"));
        var directory = NormalizeString(extendObj.GetSafeValue<string>("directory")) ?? DefaultContentDirectory;

        if (repositoryQuery == null && name != null)
        {
            SplitVersion(name, out var referenceFromName, out var tagFromName);
            if (LooksLikeGitHubReference(referenceFromName))
            {
                repositoryQuery = referenceFromName;
                tag ??= tagFromName;
            }
        }

        if (repositoryQuery == null && name == null)
        {
            throw new LunetException($"Unsupported extension/theme query [{query}]. Supports either a string or an object like {{ repo: \"owner/repo\", tag: \"v1.2.3\", directory: \"dist\" }}.");
        }

        if (repositoryQuery != null)
        {
            SplitVersion(repositoryQuery, out var repositoryWithoutVersion, out var tagFromRepository);
            tag ??= tagFromRepository;

            if (!TryNormalizeGitHubRepository(repositoryWithoutVersion, out var repository, out var owner, out var repositoryName))
            {
                throw new LunetException($"Invalid GitHub repository reference [{repositoryQuery}]. Expecting `owner/repo` or `https://github.com/owner/repo`.");
            }

            name ??= repositoryName;
            var normalizedDirectory = NormalizeDirectory(directory);
            var fullName = tag != null ? $"{repository}@{tag}" : repository;
            if (!string.Equals(normalizedDirectory, DefaultContentDirectory, StringComparison.OrdinalIgnoreCase))
            {
                fullName = $"{fullName}:{normalizedDirectory}";
            }
            return new ExtendRequest(name, fullName, repository, owner, repositoryName, tag, directory, isPrivate);
        }

        return new ExtendRequest(name, name, null, null, null, null, directory, isPrivate);
    }

    private ExtendRequest ParseQuery(string query, bool isPrivate)
    {
        var normalizedQuery = NormalizeString(query);
        if (normalizedQuery == null)
        {
            throw new LunetException("Invalid extension/theme query. Expecting either a local extension name or a GitHub repository (`owner/repo` optionally followed by `@tag`).");
        }

        SplitVersion(normalizedQuery, out var referenceWithoutVersion, out var versionFromQuery);
        if (TryNormalizeGitHubRepository(referenceWithoutVersion, out var repository, out var owner, out var repositoryName))
        {
            var fullName = versionFromQuery != null ? $"{repository}@{versionFromQuery}" : repository;
            return new ExtendRequest(repositoryName, fullName, repository, owner, repositoryName, versionFromQuery, DefaultContentDirectory, isPrivate);
        }

        return new ExtendRequest(normalizedQuery, normalizedQuery, null, null, null, null, DefaultContentDirectory, isPrivate);
    }

    private static string? NormalizeString(string? value)
    {
        if (value == null)
        {
            return null;
        }

        value = value.Trim();
        return value.Length == 0 ? null : value;
    }

    private static bool LooksLikeGitHubReference(string? query)
    {
        if (query == null)
        {
            return false;
        }

        if (query.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            query.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var parts = query.Trim('/').Split('/');
        return parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0;
    }

    private static void SplitVersion(string query, out string queryWithoutVersion, out string? version)
    {
        version = null;
        queryWithoutVersion = query;

        var atIndex = query.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == query.Length - 1)
        {
            return;
        }

        if (atIndex < query.LastIndexOf('/'))
        {
            return;
        }

        queryWithoutVersion = query.Substring(0, atIndex);
        version = query.Substring(atIndex + 1);
    }

    private static bool TryNormalizeGitHubRepository(string? repositoryQuery, out string? repository, out string? owner, out string? repositoryName)
    {
        repository = null;
        owner = null;
        repositoryName = null;

        if (repositoryQuery == null)
        {
            return false;
        }

        var query = repositoryQuery.Trim();
        if (query.Length == 0)
        {
            return false;
        }

        if (query.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            query.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(query, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            if (pathParts.Length < 2)
            {
                return false;
            }

            owner = pathParts[0].Trim();
            repositoryName = pathParts[1].Trim();
        }
        else
        {
            var pathParts = query.Trim('/').Split('/');
            if (pathParts.Length != 2)
            {
                return false;
            }

            owner = pathParts[0].Trim();
            repositoryName = pathParts[1].Trim();
        }

        if (owner.Length == 0 || repositoryName.Length == 0)
        {
            return false;
        }

        if (repositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repositoryName = repositoryName.Substring(0, repositoryName.Length - 4);
        }

        repository = $"{owner}/{repositoryName}";
        return true;
    }

    private object? ExtendFunction(object? query)
    {
        var request = ParseQuery(query);
        return LoadExtend(request);
    }

    private readonly struct ExtendRequest
    {
        public ExtendRequest(string? name, string? fullName, string? repository, string? owner, string? repositoryName, string? tag, string directory, bool isPrivate)
        {
            Name = name ?? string.Empty;
            FullName = fullName ?? Name;
            Repository = repository;
            Owner = owner;
            RepositoryName = repositoryName;
            Tag = tag;
            Directory = directory;
            IsPrivate = isPrivate;
        }

        public string Name { get; }

        public string FullName { get; }

        public string? Repository { get; }

        public string? Owner { get; }

        public string? RepositoryName { get; }

        public string? Tag { get; }

        public string Directory { get; }

        public bool IsPrivate { get; }

        public bool IsGitHub => Repository != null;

        public string? Url => IsGitHub ? $"https://github.com/{Repository}" : null;

        public string CacheKey => IsGitHub ? $"{Repository!}@{Tag ?? DefaultGitHubBranch}/{Directory}" : Name;
    }
}
