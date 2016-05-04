// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lunet.Core;
using Lunet.Helpers;
using Scriban.Runtime;

namespace Lunet.Resources
{
    /// <summary>
    /// Manages resources.
    /// </summary>
    /// <seealso cref="ManagerBase" />
    public class ResourceManager : ManagerBase
    {
        private const string ResourceDirectoryName = "resources";

        private delegate object ResourceFunctionDelegate(object o);

        public ResourceManager(SiteObject site) : base(site)
        {
            ResourceDirectory = Path.Combine(Site.Meta.Directory, ResourceDirectoryName);
            PrivateResourceDirectory = Path.Combine(Site.Meta.PrivateDirectory, ResourceDirectoryName);
            Providers = new OrderedList<ResourceProvider>()
            {
                new NpmResourceProvider(this)
            };

            site.DynamicObject.SetValue(SiteVariables.Resources, this, true);
            site.Scripts.SiteFunctions.Import(SiteVariables.ResourceFunction, (ResourceFunctionDelegate)ResourceFunction);
        }

        public FolderInfo ResourceDirectory { get; }

        public FolderInfo PrivateResourceDirectory { get; }

        public OrderedList<ResourceProvider> Providers { get; }

        public ResourceObject TryLoadResource(string providerName, string packageName, string packageVersion = null, ResourceInstallFlags flags = 0)
        {
            if (providerName == null) throw new ArgumentNullException(nameof(providerName));
            if (packageName == null) throw new ArgumentNullException(nameof(packageName));

            packageVersion = packageVersion  ?? "latest";

            foreach (var provider in Providers)
            {
                if (provider.Name == providerName)
                {
                    var resource = provider.GetOrInstall(packageName, packageVersion, flags);
                    return resource;
                }
            }

            return null;
        }

        public ResourceObject LoadResource(string resourceQuery, ResourceInstallFlags flags = 0)
        {
            if (resourceQuery == null) throw new ArgumentNullException(nameof(resourceQuery));

            var providerIndex = resourceQuery.IndexOf(':');
            if (providerIndex <= 0)
            {
                throw new LunetException($"Invalid resource name to load [{resourceQuery}]. Expecting a the character ':' between the provider name and package name (e.g: \"npm:jquery\")");
            }

            var providerName = resourceQuery.Substring(0, providerIndex);
            var packageName = resourceQuery.Substring(providerIndex+1);
            var packageVersion = "latest";

            var indexOfVersion = packageName.LastIndexOf("@", StringComparison.OrdinalIgnoreCase);
            if (indexOfVersion > 0)
            {
                packageVersion = packageName.Substring(indexOfVersion + 1);
                packageName = packageName.Substring(0, indexOfVersion);
            }

            var resource = TryLoadResource(providerName, packageName, packageVersion, flags);
            if (resource != null)
            {
                return resource;
            }

            throw new LunetException($"Unsupported provider [{providerName}] for resource \"{resourceQuery}\"");
        }

        /// <summary>
        /// The `resource` function accessible from scripts.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A ScriptObject </returns>
        /// <exception cref="LunetException">Unsupported resource parameter found. Supports either a plain string or an object with at least the properties { name: \providerName/packageName[/packageVersion]\ }</exception>
        private object ResourceFunction(object query)
        {
            var packageFullName = query as string;

            var resourceObj = query as ScriptObject;
            var flags = ResourceInstallFlags.Private;
            if (resourceObj != null)
            {
                packageFullName = resourceObj.GetSafeValue<string>("name");

                if (resourceObj.GetSafeValue<bool>("public"))
                {
                    flags = 0;
                }
                if (resourceObj.GetSafeValue<bool>("pre_release"))
                {
                    flags |= ResourceInstallFlags.PreRelease;
                }
            }

            if (packageFullName != null)
            {
                var resource = LoadResource(packageFullName, flags);
                return resource?.DynamicObject;
            }

            throw new LunetException("Unsupported resource parameter found. Supports either a plain string or an object with at least the properties { name: \"providerName:packageName[@packageVersion]\" }");
        }
    }
}