// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunet.Helpers;
using Lunet.Runtime;
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

        public ResourceManager(SiteObject site) : base(site)
        {
            ResourceDirectoryInfo = new DirectoryInfo(Path.Combine(Site.Meta.Directory, ResourceDirectoryName));
            ResourceDirectory = ResourceDirectoryInfo.FullName;

            PrivateResourceDirectoryInfo = new DirectoryInfo(Path.Combine(Site.Meta.PrivateDirectory, ResourceDirectoryName));
            PrivateResourceDirectory = PrivateResourceDirectoryInfo.FullName;
            Providers = new OrderedList<ResourceProvider>()
            {
                new NpmResourceProvider(this)
            };

            site.DynamicObject.SetValue(SiteVariables.Resources, this, true);

            site.Scripts.Context.CurrentGlobal.ImportMember(this, "Resolve", "resource");
        }

        public DirectoryInfo ResourceDirectoryInfo { get; }

        public string ResourceDirectory { get; }

        public DirectoryInfo PrivateResourceDirectoryInfo { get; }

        public string PrivateResourceDirectory { get; }

        public OrderedList<ResourceProvider> Providers { get; }

        public IDynamicObject Resolve(object query)
        {
            var packageFullName = query as string;

            var flags = ResourceInstallFlags.Private;

            if (packageFullName != null)
            {
                var names = packageFullName.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                if (names.Length < 2 || names.Length > 3)
                {
                    throw new LunetException($"Invalid resource name to load [{packageFullName}]. Expecting a `providerName/packageName[/packageVersion]` (e.g: \"npm/jquery\")");
                }
                foreach (var provider in Providers)
                {
                    if (provider.Name == names[0])
                    {
                        var packageName = names[1];
                        var packageVersion = "latest";
                        if (names.Length == 3)
                        {
                            packageVersion = names[2];
                        }
                        var resource = provider.GetOrInstall(packageName, packageVersion, flags);
                        return resource?.DynamicObject;
                    }
                }
            }

            throw new LunetException("Unsupported resource parameter found. Supports either a plain string or an object with at least the properties { name: \"providerName/packageName[/packageVersion]\" }");
        }
    }
}