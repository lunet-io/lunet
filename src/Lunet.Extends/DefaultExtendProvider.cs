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
using Scriban.Runtime;
using Zio;

namespace Lunet.Extends
{
    public class DefaultExtendProvider : IExtendProvider
    {
        private bool loaded;
        private readonly List<ExtendDescription> cacheList;

        public DefaultExtendProvider()
        {
            RegistryUrl = "https://raw.githubusercontent.com/lunet-io/lunet-registry/master/extensions.scriban";
            cacheList = new List<ExtendDescription>();
        }

        public string Name => "lunet-registry/extensions";

        public string RegistryUrl { get; set; }

        public IEnumerable<ExtendDescription> FindAll(SiteObject site)
        {
            if (loaded)
            {
                return cacheList;
            }

            if (RegistryUrl == null)
            {
                site.Error($"The registry Url for {nameof(DefaultExtendProvider)} cannot be null");
                return cacheList;
            }

            if (site.CanTrace())
            {
                site.Trace($"Checking remote registry [{RegistryUrl}] for available extensions/themes");
            }

            if (site == null) throw new ArgumentNullException(nameof(site));
            string themeRegistryStr;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    themeRegistryStr = client.GetStringAsync(RegistryUrl).Result;
                }
            }
            catch (Exception ex)
            {
                site.Error(ex, $"Unable to load theme registry from Url [{RegistryUrl}]. Reason:{ex.GetReason()}");
                return cacheList;
            }

            var registryObject = new DynamicObject();
            object result;
            if (site.Scripts.TryImportScript(themeRegistryStr, Name, registryObject, ScriptFlags.None, out result))
            {
                var themeList = registryObject["extensions"] as ScriptArray;
                if (themeList != null)
                {
                    foreach (var theme in themeList)
                    {
                        var themeObject = theme as ScriptObject;
                        if (themeObject != null)
                        {
                            var themeName = (string) themeObject["name"];
                            var themeDescription = (string) themeObject["description"];
                            var themeUrl = (string) themeObject["url"];
                            var themeDirectory = (string)themeObject["directory"];
                            cacheList.Add(new ExtendDescription(themeName, themeDescription, themeUrl, themeDirectory));
                        }
                    }
                }
            }

            loaded = true;
            return cacheList;
        }

        public bool TryInstall(SiteObject site, string extend, string version, IFileSystem outputFileSystem)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                version = "master";
            }

            foreach (var themeDesc in FindAll(site))
            {
                var fullVersion = themeDesc.Url + "/archive/" + version + ".zip";
                if (themeDesc.Name == extend)
                {
                    try
                    {
                        if (site.CanInfo())
                        {
                            site.Info($"Downloading and installing extension/theme `{extend}` to `{outputFileSystem.ConvertPathToInternal(UPath.Root)}`");
                        }

                        using (HttpClient client = new HttpClient())
                        {
                            using (var stream = client.GetStreamAsync(fullVersion).Result)
                            {
                                //site.Content.CreateDirectory(new DirectoryEntry(site.FileSystem, outputFileSystem));

                                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                                {
                                    var indexOfRepoName = themeDesc.Url.LastIndexOf('/');
                                    string repoName = themeDesc.Url.Substring(indexOfRepoName + 1);
                                    var directoryInZip = $"{repoName}-{version}/{themeDesc.Directory}";
                                    zip.ExtractToDirectory(new DirectoryEntry(outputFileSystem, UPath.Root), directoryInZip);
                                }
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        site.Error(ex, $"Unable to load extension/theme from Url [{fullVersion}]. Reason:{ex.GetReason()}");
                        break;
                    }
                }
            }
            return false;
        }
    }
}