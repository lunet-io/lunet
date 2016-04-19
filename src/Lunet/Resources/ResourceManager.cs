// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            site.Scripts.GlobalObject.Import("resource", (ResourceFunctionDelegate)ResourceFunction);
        }

        public FolderInfo ResourceDirectory { get; }

        public FolderInfo PrivateResourceDirectory { get; }

        public OrderedList<ResourceProvider> Providers { get; }

        public ResourceObject LoadResource(string resourceQuery, ResourceInstallFlags flags = 0)
        {
            if (resourceQuery == null) throw new ArgumentNullException(nameof(resourceQuery));

            var queryParts = resourceQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (queryParts.Length < 2 || queryParts.Length > 3)
            {
                throw new LunetException($"Invalid resource name to load [{resourceQuery}]. Expecting a `providerName/packageName[/packageVersion]` (e.g: \"npm/jquery\")");
            }

            var providerName = queryParts[0];
            var packageName = queryParts[1];

            foreach (var provider in Providers)
            {
                if (provider.Name == providerName)
                {
                    var packageVersion = "latest";
                    if (queryParts.Length == 3)
                    {
                        packageVersion = queryParts[2];
                    }
                    var resource = provider.GetOrInstall(packageName, packageVersion, flags);
                    return resource;
                }
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

            throw new LunetException("Unsupported resource parameter found. Supports either a plain string or an object with at least the properties { name: \"providerName/packageName[/packageVersion]\" }");
        }
    }
}