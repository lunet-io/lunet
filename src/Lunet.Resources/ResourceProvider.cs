// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Scriban.Runtime;
using Zio;

namespace Lunet.Resources;

public abstract class ResourceProvider
{
    protected ResourceProvider(ResourcePlugin plugin, string name)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (name == null) throw new ArgumentNullException(nameof(name));
        Plugin = plugin;
        Name = name;
        Resources = new List<ResourceObject>();

        // Set the list of resources loaded
        plugin.SetValue(name, ResourcesForScripting, true);
    }

    public ResourcePlugin Plugin { get; }

    public string Name { get; }

    public List<ResourceObject> Resources { get; }

    public IEnumerable<ResourceObject> ResourcesForScripting
    {
        get
        {
            foreach (var resource in Resources)
            {
                yield return resource;
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

    private void LoadFromDirectory(DirectoryEntry resourceDirectory)
    {
        foreach (var resourceNameDir in resourceDirectory.EnumerateDirectories())
        {
            foreach (var versionNameDir in resourceNameDir.EnumerateDirectories())
            {
                var resource = Find(resourceNameDir.Name, versionNameDir.Name);
                if (resource == null)
                {
                    resource = LoadFromDisk(resourceNameDir.Name, versionNameDir.Name, versionNameDir);
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
        var resourcePrivatePath = new DirectoryEntry(Plugin.Site.CacheMetaFileSystem, ResourcePlugin.ResourceFolder / Name / resourceName / resourceVersion);
        var resourcePublicPath = new DirectoryEntry(Plugin.Site.MetaFileSystem, ResourcePlugin.ResourceFolder / Name / resourceName / resourceVersion);
        DirectoryEntry resourcePath = null;
        if (resourcePublicPath.Exists)
        {
            resourcePath = resourcePublicPath;
        }
        else if (resourcePrivatePath.Exists)
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


    protected abstract ResourceObject LoadFromDisk(string resourceName, string resourceVersion, DirectoryEntry directory);

    protected abstract ResourceObject InstallToDisk(string resourceName, string resourceVersion, DirectoryEntry directory, ResourceInstallFlags flags);
}