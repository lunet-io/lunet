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
    public class ExtendManager : ManagerBase
    {
        private const string ExtendsDirectoryName = "extends";

        private delegate object ExtendFunctionDelegate(object o);

        public ExtendManager(SiteObject site) : base(site)
        {
            Providers = new OrderedList<IExtendProvider>();

            ExtendDirectory = Path.Combine(Site.Meta.Directory, ExtendsDirectoryName);
            PrivateExtendDirectory = Path.Combine(Site.Meta.PrivateDirectory, ExtendsDirectoryName);
            CurrentList = new List<ExtendObject>();
            Providers = new OrderedList<IExtendProvider>()
            {
                new DefaultExtendProvider()
            };

            site.Scripts.GlobalObject.SetValue(SiteVariables.Extends, CurrentList.AsReadOnly(), true);
            site.Scripts.SiteFunctions.Import(SiteVariables.ExtendFunction, (ExtendFunctionDelegate)ExtendFunction);
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
            Site.Scripts.TryImportScriptFromFile(configPath, Site.DynamicObject, ScriptFlags.AllowSiteFunctions);

            return extendObject;
        }

        public ExtendObject TryInstall(string extend, bool isPrivate = false)
        {
            if (extend == null) throw new ArgumentNullException(nameof(extend));

            var themePrivatePath = Path.Combine(PrivateExtendDirectory, extend);
            var themePublicPath = Path.Combine(ExtendDirectory, extend);
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
                return new ExtendObject(Site, new ExtendDescription(extend, null, null, null), extendPath);
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
                    if (extendDesc.Provider.TryInstall(Site, extend, extendPath))
                    {
                        return new ExtendObject(Site, extendDesc, extendPath);
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
                return resource?.DynamicObject;
            }

            throw new LunetException($"Unsupported extension/theme name [{query}]");
        }
    }
}