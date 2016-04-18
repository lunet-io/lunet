// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Scriban.Runtime;
using Lunet.Helpers;
using Lunet.Runtime;

namespace Lunet.Themes
{
    public class DefaultThemeProvider : IThemeProvider
    {
        public DefaultThemeProvider()
        {
            RegistryUrl = "https://raw.githubusercontent2.com/lunet-io/lunet-registry/master/themes.sban";
        }

        public string Name => "lunet-registry/themes";

        public string RegistryUrl { get; set; }

        public IEnumerable<ThemeDescription> FindAll(SiteObject site)
        {
            if (RegistryUrl == null)
            {
                site.Error($"The registry Url for {nameof(DefaultThemeProvider)} cannot be null");
                yield break;
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
                site.Error($"Unable to load theme registry from Url [{RegistryUrl}]. Reason:{ex.GetReason()}");
                yield break;
            }

            var themes = new ScriptObject();
            if (site.Scripts.TryImportScript(themeRegistryStr, Name, themes))
            {
                var themeList = themes["themes"] as ScriptArray;
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
                            yield return new ThemeDescription(themeName, themeDescription, themeUrl, themeDirectory);
                        }
                    }
                }
            }
        }

        public bool TryInstall(SiteObject site, string theme, string outputPath)
        {
            if (site.CanTrace())
            {
                site.Trace($"Checking remove registry [{RegistryUrl}] for available themes  Installing theme [{theme}] to [{site.GetRelativePath(outputPath)}]");
            }

            foreach (var themeDesc in FindAll(site))
            {
                if (themeDesc.Name == theme)
                {
                    try
                    {
                        if (site.CanTrace())
                        {
                            site.Trace($"Downloading theme Installing theme [{theme}] to [{site.GetRelativePath(outputPath)}]");
                        }

                        using (HttpClient client = new HttpClient())
                        {
                            using (var stream = client.GetStreamAsync(themeDesc.Url).Result)
                            {
                                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                                {
                                    zip.ExtractToDirectory(outputPath, themeDesc.Directory);
                                }
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        site.Error($"Unable to load theme registry from Url [{RegistryUrl}]. Reason:{ex.GetReason()}");
                        break;
                    }
                }
            }
            return false;
        }
    }
}