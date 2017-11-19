// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Scripts;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Extends
{
    /// <summary>
    /// Manages themes.
    /// </summary>
    public sealed class ExtendsPlugin : SitePlugin
    {
        private const string ExtendsFolderName = "extends";

        private delegate object ExtendFunctionDelegate(object o);

        public ExtendsPlugin(SiteObject site) : base(site)
        {
            ExtendsFolder = UPath.Root / ExtendsFolderName;
            PrivateExtendsFolder = UPath.Root / ExtendsFolderName;
            Providers = new OrderedList<IExtendProvider>()
            {
                new DefaultExtendProvider()
            };
            CurrentList = new List<ExtendObject>();
            Site.Scripts.GlobalObject.SetValue(SiteVariables.Extends, CurrentList.AsReadOnly(), true);
            Site.Scripts.SiteFunctions.Import(SiteVariables.ExtendFunction, (ExtendFunctionDelegate)ExtendFunction);
        }

        public UPath ExtendsFolder { get; }

        public UPath PrivateExtendsFolder { get; }

        public OrderedList<IExtendProvider> Providers { get; }

        /// <summary>
        /// Gets the list of themes currently used.
        /// </summary>
        public List<ExtendObject> CurrentList { get; }

        public IEnumerable<ExtendDescription> FindAll()
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

        public ExtendObject LoadExtend(string extendName, bool isPrivate)
        {
            if (extendName == null) throw new ArgumentNullException(nameof(extendName));
            ExtendObject extendObject = null;
            foreach (var existingExtend in CurrentList)
            {
                if (existingExtend.Name == extendName)
                {
                    extendObject = existingExtend;
                    break;
                }
            }

            if (extendObject == null)
            {
                extendObject = TryInstall(extendName, isPrivate);
                if (extendObject == null)
                {
                    return null;
                }
                CurrentList.Add(extendObject);

                var configPath = new FileEntry(extendObject.FileSystem, UPath.Root / SiteObject.DefaultConfigFileName);
                object result;
                Site.Scripts.TryImportScriptFromFile(configPath, Site, ScriptFlags.AllowSiteFunctions, out result);
            }

            // Register the extensions as a content FileSystem
            Site.AddContentFileSystem(extendObject.FileSystem);

            if (Site.CanTrace())
            {
                Site.Trace($"Using extension/theme `{extendName}` from `{extendObject.Directory}`");
            }

            return extendObject;
        }

        public ExtendObject TryInstall(string extendName, bool isPrivate = false)
        {
            if (extendName == null) throw new ArgumentNullException(nameof(extendName));
            var extend = extendName;

            string version = null;
            var indexOfVersion = extend.IndexOf('@');
            if (indexOfVersion > 0)
            {
                extend = extend.Substring(0, indexOfVersion);
                version = extend.Substring(indexOfVersion + 1);
            }

            //var themePrivatePath = PrivateExtendsFolder / extendName;
            var themePublicPath = Site.MetaFileSystem.GetDirectoryEntry(ExtendsFolder / extendName);
            throw new NotSupportedException();
            var themePrivatePath = themePublicPath;
            IFileSystem extendPath = null;

            if (themePublicPath.Exists)
            {
                extendPath = new SubFileSystem(themePublicPath.FileSystem, themePublicPath.Path);
            }
            //else if (Directory.Exists(themePrivatePath))
            //{
            //    extendPath = themePrivatePath;
            //}

            if (extendPath != null)
            {
                return new ExtendObject(Site, extendName, extend, version, null, null, extendPath);
            }

            //extendPath = isPrivate ? themePrivatePath : themePublicPath;

            if (Providers.Count == 0)
            {
                Site.Error($"Unable to find the extension/theme [{extend}]. No provider list installed.");
                return null;
            }

            foreach (var extendDesc in FindAll())
            {
                if (extendDesc.Name == extend)
                {
                    if (extendDesc.Provider.TryInstall(Site, extend, version, extendPath))
                    {
                        return new ExtendObject(Site, extendName, extend, version, extendDesc.Description, extendDesc.Url, extendPath);
                    }
                    return null;
                }
            }

            Site.Error($"Unable to find the extension/theme [{extend}] locally from [{themePublicPath}] or [{themePrivatePath}] or from the provider list [{string.Join(",", Providers.Select(t => t.Name))}]");
            return null;
        }

        private object ExtendFunction(object query)
        {
            var extendName = query as string;

            var extendObj = query as ScriptObject;
            bool isPrivate = true;
            if (extendObj != null)
            {
                extendName = extendObj.GetSafeValue<string>("name");

                if (extendObj.GetSafeValue<bool>("public"))
                {
                    isPrivate = false;
                }
            }

            if (extendName != null)
            {
                var resource = LoadExtend(extendName, isPrivate);
                return resource;
            }

            throw new LunetException($"Unsupported extension/theme name [{query}]");
        }
    }
}