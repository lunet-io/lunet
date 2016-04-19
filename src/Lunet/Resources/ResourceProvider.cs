// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;

namespace Lunet.Resources
{
    public abstract class ResourceProvider
    {
        protected ResourceProvider(ResourceManager manager, string name)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (name == null) throw new ArgumentNullException(nameof(name));
            Manager = manager;
            Name = name;
            Resources = new List<ResourceObject>();

            // Set the list of resources loaded
            manager.DynamicObject.SetValue(name, ResourcesForScripting, true);
        }

        public ResourceManager Manager { get; }

        public string Name { get; }

        public List<ResourceObject> Resources { get; }

        public IEnumerable<IDynamicObject> ResourcesForScripting
        {
            get
            {
                foreach (var resource in Resources)
                {
                    yield return resource.DynamicObject;
                }
            }
        }

        public ResourceObject Find(string resourceName, string resourceVersion)
        {
            if (resourceName == null) throw new ArgumentNullException(nameof(resourceName));
            if (resourceVersion == null) throw new ArgumentNullException(nameof(resourceVersion));

            // If we have already loaded a resource, return it immediately
            foreach (var existingResource in Resources)
            {
                if (existingResource.Name == resourceName && existingResource.Version == resourceVersion)
                {
                    return existingResource;
                }
            }
            return null;
        }

        public string FindFromDirectory(string resourceName, string resourceVersion)
        {
            var resourcePrivatePath = Path.Combine(Manager.PrivateResourceDirectory, Name, resourceName, resourceVersion);
            var resourcePublicPath = Path.Combine(Manager.ResourceDirectory, Name, resourceName, resourceVersion);
            string directory = null;
            if (Directory.Exists(resourcePublicPath))
            {
                directory = resourcePublicPath;
            }
            else if (Directory.Exists(resourcePrivatePath))
            {
                directory = resourcePrivatePath;
            }
            return directory;
        }

        private void LoadFromDirectory(DirectoryInfo resourceDirectory)
        {
            foreach (var resourceNameDir in resourceDirectory.EnumerateDirectories())
            {
                foreach (var versionNameDir in resourceNameDir.EnumerateDirectories())
                {
                    var resource = Find(resourceNameDir.Name, versionNameDir.Name);
                    if (resource == null)
                    {
                        resource = LoadFromDisk(resourceNameDir.Name, versionNameDir.Name, versionNameDir.FullName);
                        if (resource != null)
                        {
                            Resources.Add(resource);
                        }
                    }
                }
            }
        }

        public ResourceObject GetOrInstall(string resourceName, string resourceVersion, ResourceInstallFlags flags)
        {
            if (resourceName == null) throw new ArgumentNullException(nameof(resourceName));
            if (resourceVersion == null) throw new ArgumentNullException(nameof(resourceVersion));

            // Returns an existing resource if any
            var resource = Find(resourceName, resourceVersion);
            if (resource != null)
            {
                return resource;
            }

            // Otherwise we are going to check if it is already on the disk

            var resourcePrivatePath = Path.Combine(Manager.PrivateResourceDirectory, Name, resourceName, resourceVersion);
            var resourcePublicPath = Path.Combine(Manager.ResourceDirectory, Name, resourceName, resourceVersion);

            string resourcePath = FindFromDirectory(resourceName, resourceVersion);
            if (Directory.Exists(resourcePublicPath))
            {
                resourcePath = resourcePublicPath;
            }
            else if (Directory.Exists(resourcePrivatePath))
            {
                resourcePath = resourcePrivatePath;
            }

            if (resourcePath != null)
            {
                resource = LoadFromDisk(resourceName, resourceVersion, resourcePath);
            }
            else if ((flags & ResourceInstallFlags.NoInstall) == 0)
            {
                resourcePath = (flags & ResourceInstallFlags.Private) != 0 ? resourcePrivatePath : resourcePublicPath;
                resource = InstallToDisk(resourceName, resourceVersion, resourcePath, flags);
            }

            if (resource != null && !Resources.Contains(resource))
            {
                Resources.Add(resource);
            }
            return resource;
        }


        protected abstract ResourceObject LoadFromDisk(string resourceName, string resourceVersion, string directory);

        protected abstract ResourceObject InstallToDisk(string resourceName, string resourceVersion, string directory, ResourceInstallFlags flags);
    }
}