// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Scripts;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using Scriban;
using Scriban.Runtime;
using Zio;

namespace Lunet.Resources
{
    public class NpmResourceProvider : ResourceProvider
    {
        public NpmResourceProvider(ResourcePlugin plugin) : base(plugin, "npm")
        {
            RegistryUrl = "https://registry.npmjs.org/";
        }

        public string RegistryUrl { get; set; }

        protected override ResourceObject LoadFromDisk(string resourceName, string resourceVersion, DirectoryEntry directory)
        {
            // Imports the json properties into the runtime object
            var packageJson = new FileEntry(directory.FileSystem, directory.Path / "package.json");
            if (!packageJson.Exists)
            {
                Plugin.Site.Error($"The [{Name}] package doesn't contain the file `{packageJson}`");
                return null;
            }

            var resource = new ResourceObject(this, resourceName, resourceVersion, directory);

            object result;
            Plugin.Site.Scripts.TryImportScriptFromFile(packageJson, resource, ScriptFlags.Expect, out result);
            if (result is ScriptObject)
            {
                var dynamicObject = (DynamicObject) resource;
                dynamicObject.Import(result);
                var main = dynamicObject.GetSafeValue<string>("main");
                if (main != null)
                {
                    dynamicObject["main"] = PathUtil.NormalizeRelativePath(Path.Combine(resource.Path, main), false);
                }
            }

            return resource;
        }

        protected override ResourceObject InstallToDisk(string resourceName, string resourceVersion, DirectoryEntry directory, ResourceInstallFlags flags)
        {
            JObject resourceJson = null;
            var npmPackageUrl = RegistryUrl + resourceName;
            try
            {
                using (var client = new HttpClient())
                {
                    var resourceRegistryStr = client.GetStringAsync(npmPackageUrl).Result;
                    resourceJson = JObject.Parse(resourceRegistryStr);
                }
            }
            catch (Exception ex)
            {
                Plugin.Site.Error(
                    $"Unable to load a [{Name}] resource from registry from Url [{npmPackageUrl}]. Reason:{ex.GetReason()}");
                return null;
            }


            var versions = resourceJson["versions"] as JObject;
            if (versions == null)
            {
                Plugin.Site.Error(
                    $"Unable to find `versions` property from [{Name}] package from Url [{npmPackageUrl}]");
                return null;
            }

            var downloads = new Dictionary<string, string>();
            string selectedVersion = null;
            SemanticVersion lastVersion = null;
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
                            SemanticVersion version;
                            if (SemanticVersion.TryParse(versionName, out version))
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

                // Otherwise, we have to donwload the package and unzip it
                var downloadUrl = downloads[selectedVersion];
                try
                {
                    using (var client = new HttpClient())
                    {
                        // TODO: check if the file is ending by tgz/tat.gz?
                        using (var stream = client.GetStreamAsync(downloadUrl).Result)
                        using (var gzStream = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            gzStream.UntarTo(directory, "package");
                        }

                        return LoadFromDisk(resourceName, selectedVersion, directory);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Site.Error(
                        $"Unable to download and install the [{Name}] package [{resourceName}/{resourceVersion}] from the url [{downloadUrl}]. Reason:{ex.GetReason()}");
                    return null;
                }
            }

            Plugin.Site.Error($"Unable to find the [{Name}] package [{resourceName}] with the specific version [{resourceVersion}] from the available version [{string.Join(",", downloads.Keys)}]");
            return null;
        }
    }
}