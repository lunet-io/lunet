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

namespace Lunet.Extends
{
    /// <summary>
    /// Manages themes.
    /// </summary>
    /// <seealso cref="ManagerBase" />
    public sealed class ExtendManager : ManagerBase
    {
        private const string ExtendsDirectoryName = "extends";

        private delegate object ExtendFunctionDelegate(object o);

        public ExtendManager(SiteObject site) : base(site)
        {
            ExtendDirectory = Path.Combine(Site.Meta.Directory, ExtendsDirectoryName);
            PrivateExtendDirectory = Path.Combine(Site.Meta.PrivateDirectory, ExtendsDirectoryName);
            Providers = new OrderedList<IExtendProvider>()
            {
                new DefaultExtendProvider()
            };
            CurrentList = new List<ExtendObject>();
            Site.Scripts.GlobalObject.SetValue(SiteVariables.Extends, CurrentList.AsReadOnly(), true);
            Site.Scripts.SiteFunctions.Import(SiteVariables.ExtendFunction, (ExtendFunctionDelegate)ExtendFunction);
        }

        public FolderInfo ExtendDirectory { get; }

        public FolderInfo PrivateExtendDirectory { get; }

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
            foreach (var existingExtend in CurrentList)
            {
                if (existingExtend.Name == extendName)
                {
                    return existingExtend;
                }
            }

            var extendObject = TryInstall(extendName, isPrivate);
            if (extendObject == null)
            {
                return null;
            }
            CurrentList.Add(extendObject);

            if (Site.CanTrace())
            {
                Site.Trace($"Using extension/theme [{extendName}] from [{extendObject.Path}]");
            }

            var configPath = Path.Combine(extendObject.Directory, SiteFactory.DefaultConfigFilename);
            Site.Scripts.TryImportScriptFromFile(configPath, Site, ScriptFlags.AllowSiteFunctions);

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

            var themePrivatePath = Path.Combine(PrivateExtendDirectory, extendName);
            var themePublicPath = Path.Combine(ExtendDirectory, extendName);
            string extendPath = null;
            if (Directory.Exists(themePublicPath))
            {
                extendPath = themePublicPath;
            }
            else if (Directory.Exists(themePrivatePath))
            {
                extendPath = themePrivatePath;
            }

            if (extendPath != null)
            {
                return new ExtendObject(Site, extendName, extend, version, null, null, extendPath);
            }

            extendPath = isPrivate ? themePrivatePath : themePublicPath;

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

            Site.Error($"Unable to find the extension/theme [{extend}] locally from [{Site.GetRelativePath(themePublicPath, PathFlags.File)}] or [{Site.GetRelativePath(themePrivatePath, PathFlags.File)}] or from the provider list [{string.Join(",", Providers.Select(t => t.Name))}]");
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