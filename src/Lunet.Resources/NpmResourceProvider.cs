// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Scripts;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using Scriban;
using Scriban.Runtime;
using Zio;

namespace Lunet.Resources;

public class NpmResourceProvider : ResourceProvider
{
    internal const string RequestTimeoutKey = "npm_request_timeout";

    public NpmResourceProvider(ResourcePlugin plugin) : base(plugin, "npm")
    {
        RegistryUrl = "https://registry.npmjs.org/";
    }

    public string RegistryUrl { get; set; }

    /// <summary>
    /// Gets or sets the timeout for each registry request or complete tarball download.
    /// Defaults to 100 seconds. Archive extraction is not included.
    /// Shares its value with <c>resources.npm_request_timeout</c>, expressed in seconds in scripts.
    /// </summary>
    /// <exception cref="LunetException">The configured script value is not a valid timeout.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not positive or exceeds <see cref="int.MaxValue"/> milliseconds.
    /// </exception>
    public TimeSpan RequestTimeout
    {
        get
        {
            var seconds = Plugin[RequestTimeoutKey] switch
            {
                int value => value,
                long value => value,
                float value => value,
                double value => value,
                decimal value => (double)value,
                _ => double.NaN,
            };
            if (seconds > 0 && seconds <= int.MaxValue / 1000.0)
            {
                var timeout = TimeSpan.FromSeconds(seconds);
                if (timeout > TimeSpan.Zero) return timeout;
            }

            throw new LunetException("Invalid resources.npm_request_timeout: expected a positive, finite number of seconds, no greater than 2147483.647.");
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, TimeSpan.FromMilliseconds(int.MaxValue));
            Plugin.SetValue(RequestTimeoutKey, value.TotalSeconds);
        }
    }

    protected override ResourceObject? LoadFromDisk(string resourceName, string resourceVersion, DirectoryEntry directory)
    {
        // Imports the json properties into the runtime object
        var packageJson = new FileEntry(directory.FileSystem, directory.Path / "package.json");
        if (!packageJson.Exists)
        {
            Plugin.Site.Error($"The [{Name}] package doesn't contain the file `{packageJson}`");
            return null;
        }

        var resource = new ResourceObject(this, resourceName, resourceVersion, directory);

        object? result;
        Plugin.Site.Scripts.TryImportScriptFromFile(packageJson, resource, ScriptFlags.Expect, out result);
        if (result is ScriptObject)
        {
            var dynamicObject = (DynamicObject) resource;
            dynamicObject.Import(result);
            var main = dynamicObject.GetSafeValue<string>("main");
            if (main != null)
            {
                var mainPath = UPath.Combine(resource.Path, main);
                dynamicObject["main"] = mainPath;
            }
        }

        return resource;
    }

    protected override ResourceObject? InstallToDisk(string resourceName, string resourceVersion, DirectoryEntry directory, ResourceInstallFlags flags)
    {
        Plugin.Site.Info($"NPM installing {resourceName}");

        JObject? resourceJson = null;
        var npmPackageUrl = RegistryUrl + resourceName;
        try
        {
            using (var client = new HttpClient { Timeout = RequestTimeout })
            {
                var resourceRegistryStr = client.GetStringAsync(npmPackageUrl).GetAwaiter().GetResult();
                resourceJson = JObject.Parse(resourceRegistryStr);
            }
        }
        catch (Exception ex)
        {
            Plugin.Site.Error(ex, $"Unable to load a [{Name}] resource from registry from Url [{npmPackageUrl}]. Reason:{ex.GetReason()}");
            return null;
        }

        if (resourceJson is null)
        {
            return null;
        }

        var versions = resourceJson["versions"] as JObject;
        if (versions == null)
        {
            Plugin.Site.Error($"Unable to find `versions` property from [{Name}] package from Url [{npmPackageUrl}]");
            return null;
        }

        var downloads = new Dictionary<string, string>();
        string? selectedVersion = null;
        SemanticVersion? lastVersion = null;
        bool isLatestVersion = false;

        foreach (var prop in versions.Properties())
        {
            var versionName = prop.Name;
            var versionValue = prop.Value as JObject;
            if (versionName != null && versionValue != null)
            {
                var downloadUrl = ((versionValue["dist"] as JObject)?["tarball"] as JValue)?.Value as string;

                if (downloadUrl != null)
                {
                    downloads[versionName] = downloadUrl;

                    if (versionName == resourceVersion)
                    {
                        selectedVersion = resourceVersion;
                    }
                    else if (resourceVersion == "latest")
                    {
                        SemanticVersion? version;
                        if (SemanticVersion.TryParse(versionName, out version) && version is not null)
                        {
                            if (!version.IsPrerelease || (flags & ResourceInstallFlags.PreRelease) != 0)
                            {
                                if (lastVersion == null)
                                {
                                    lastVersion = version;
                                    selectedVersion = versionName;
                                }
                                else if (version > lastVersion)
                                {
                                    lastVersion = version;
                                    selectedVersion = versionName;
                                }
                                isLatestVersion = true;
                            }
                        }
                    }
                }
            }
        }

        if (selectedVersion != null)
        {
            // In case of a latest version, we will try to load the version from the disk if it is already installed
            if (isLatestVersion)
            {
                var resource = GetOrInstall(resourceName, selectedVersion, flags | ResourceInstallFlags.NoInstall);
                if (resource != null)
                {
                    return resource;
                }
            }

            // Otherwise, we have to download the package and unzip it
            var downloadUrl = downloads[selectedVersion];
            try
            {
                using (var client = new HttpClient { Timeout = RequestTimeout })
                {
                    Plugin.Site.Info($"NPM downloading {resourceName}/{selectedVersion} from [{downloadUrl}]");

                    // Stage the complete download before extracting: synchronous tar reads cannot
                    // cancel a stalled network stream, and partial transfers must not populate the cache.
                    using var archive = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
                        FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920,
                        FileOptions.Asynchronous | FileOptions.DeleteOnClose);
                    DownloadArchiveAsync(client, downloadUrl, archive).GetAwaiter().GetResult();
                    archive.Position = 0;

                    Plugin.Site.Info($"NPM extracting {resourceName}/{selectedVersion}");
                    using (var gzStream = new GZipStream(archive, CompressionMode.Decompress))
                    {
                        gzStream.UntarTo(directory, "package");
                    }

                    return LoadFromDisk(resourceName, selectedVersion, directory);
                }
            }
            catch (Exception ex)
            {
                Plugin.Site.Error(ex, $"Unable to download and install the [{Name}] package [{resourceName}/{resourceVersion}] from the url [{downloadUrl}]. Reason:{ex.GetReason()}");
                return null;
            }
        }

        Plugin.Site.Error($"Unable to find the [{Name}] package [{resourceName}] with the specific version [{resourceVersion}] from the available version [{string.Join(",", downloads.Keys)}]");
        return null;
    }

    private static async Task DownloadArchiveAsync(HttpClient client, string downloadUrl, Stream destination)
    {
        // GetStreamAsync/ResponseHeadersRead only apply HttpClient.Timeout through the headers.
        // Keep a separate deadline alive through the entire response body as well.
        using var timeout = new CancellationTokenSource(client.Timeout);
        try
        {
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(destination, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"NPM download timed out after {client.Timeout.TotalSeconds} seconds.", ex);
        }
    }
}
