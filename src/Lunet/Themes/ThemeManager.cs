// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunet.Helpers;
using Lunet.Runtime;

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

            var themePrivatePath = Path.Combine(PrivateThemeDirectory, theme);
            var themePublicPath = Path.Combine(ThemeDirectory, theme);
            string themePath = null;
            if (Directory.Exists(themePublicPath))
            {
                themePath = themePublicPath;
            }
            else if (Directory.Exists(themePrivatePath))
            {
                themePath = themePrivatePath;
            }

            if (themePath != null)
            {
                return new ThemeObject(Site, new ThemeDescription(theme, null, null, null), themePath);
            }

            themePath = isPrivate ? themePrivatePath : themePublicPath;

            if (Providers.Count == 0)
            {
                Site.Error($"Unable to find the theme [{theme}]. No provider list installed.");
                return null;
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

            Site.Error($"Unable to find the theme [{theme}] locally from [{Site.GetRelativePath(themePublicPath)}] or [{Site.GetRelativePath(themePrivatePath)}] or from the provider list [{string.Join(",", Providers.Select(t => t.Name))}]");
            return null;
        }

        public override void InitializeAfterConfig()
        {
            var theme = Site.GetSafe<string>(SiteVariables.Theme);
            var defaultTheme = theme;

            var themeLoaded = new HashSet<string>();
            var themeText = "theme";
            while (theme != null)
            {
                themeLoaded.Add(theme);

                var themeObject = TryInstall(theme);
                if (themeObject == null)
                {
                    break;
                }

                if (Site.CanTrace())
                {
                    Site.Trace($"Using {themeText} [{theme}] from [{themeObject.Path}]");
                }

                var configPath = Path.Combine(themeObject.Directory, SiteFactory.DefaultConfigFileName1);
                if (Site.Scripts.TryImportScriptFromFile(configPath, Site))
                {
                    // Retrieve the theme from the page
                    // If we have a new theme proceed to inherited theme
                    var nextTheme = Site.GetSafe<string>(SiteVariables.Theme);
                    theme = nextTheme != theme ? nextTheme : null;

                    if (theme != null)
                    {
                        if (themeLoaded.Contains(theme))
                        {
                            Site.Error($"Invalid recursive theme [{theme}] loaded from [{Site.GetRelativePath(configPath)}");
                            break;
                        }
                        themeText = "inherited theme";
                    }
                }
                else
                {
                    theme = null;
                }

                CurrentList.Add(themeObject);
            }

            // Restore the value of the theme
            if (defaultTheme != null)
            {
                Site.SetValue(SiteVariables.Theme, defaultTheme, false);
            }
        }
    }
}