// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunet.Helpers;
using Lunet.Runtime;
using Microsoft.Extensions.Logging;

namespace Lunet.Themes
{
    /// <summary>
    /// Manages themes.
    /// </summary>
    /// <seealso cref="ManagerBase" />
    public class ThemeManager : ManagerBase
    {
        private const string ThemesDirectoryName = "themes";

        public ThemeManager(SiteObject site) : base(site)
        {
            Providers = new OrderedList<IThemeProvider>();

            ThemeDirectoryInfo = new DirectoryInfo(Path.Combine(Site.Meta.Directory, ThemesDirectoryName));
            ThemeDirectory = ThemeDirectoryInfo.FullName;

            PrivateThemeDirectoryInfo = new DirectoryInfo(Path.Combine(Site.Meta.PrivateDirectory, ThemesDirectoryName));
            PrivateThemeDirectory = PrivateThemeDirectoryInfo.FullName;
            CurrentList = new List<ThemeObject>();
            Providers = new OrderedList<IThemeProvider>()
            {
                new DefaultThemeProvider()
            };
        }

        public DirectoryInfo ThemeDirectoryInfo { get; }

        public string ThemeDirectory { get; }

        public DirectoryInfo PrivateThemeDirectoryInfo { get; }

        public string PrivateThemeDirectory { get; }

        public OrderedList<IThemeProvider> Providers { get; }

        /// <summary>
        /// Gets the list of themes currently used.
        /// </summary>
        public List<ThemeObject> CurrentList { get; }

        public IEnumerable<ThemeDescription> FindAll()
        {
            foreach (var provider in Providers)
            {
                foreach (var desc in provider.FindAll(Site))
                {
                    var copyDesc = desc;
                    copyDesc.Provider = provider;
                    yield return copyDesc;
                }
            }
        }

        public ThemeObject TryInstall(string theme, bool isPrivate = false)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));

            if (Providers.Count == 0)
            {
                Site.Log.LogError($"Unable to find the theme [{theme}]. No provider list installed.");
                return null;
            }

            var themePath = Path.Combine(isPrivate ? PrivateThemeDirectory : ThemeDirectory, theme);
            if (Directory.Exists(themePath))
            {
                return new ThemeObject(Site, new ThemeDescription(theme, null, null, null), themePath);
            }

            foreach (var themeDesc in FindAll())
            {
                if (themeDesc.Name == theme)
                {
                    if (themeDesc.Provider.TryInstall(Site, theme, themePath))
                    {
                        return new ThemeObject(Site, themeDesc, themePath);
                    }
                    return null;
                }
            }

            Site.Log.LogError($"Unable to find the theme [{theme}] from the provider list [{string.Join(",", Providers.Select(t => t.Name))}]");
            return null;
        }

        public override void InitializeAfterConfig()
        {
            var theme = Site.GetSafe<string>(SiteVariables.Theme);

            if (theme != null)
            {
                string themePath = null;

                themePath = Path.Combine(ThemeDirectory, theme);
                if (!Directory.Exists(themePath))
                {
                    themePath = Path.Combine(PrivateThemeDirectory, theme);
                    if (!Directory.Exists(themePath))
                    {
                        themePath = null;
                    }
                }

                if (themePath == null)
                {
                    var installedTheme = TryInstall(theme, true);
                    if (installedTheme != null)
                    {
                        CurrentList.Add(installedTheme);
                    }
                }
            }
        }
    }
}