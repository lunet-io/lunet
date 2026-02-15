// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Lunet.Core;
using Lunet.Helpers;
using Scriban.Runtime;
using Zio;

namespace Lunet.Resources;

public class ResourceModule : SiteModule<ResourcePlugin>
{
}
    
/// <summary>
/// Manages resources.
/// </summary>
public sealed class ResourcePlugin : SitePlugin
{
    private const string ResourceFolderName = "resources";
    public static readonly UPath ResourceFolder = UPath.Root / ResourceFolderName;

    public ResourcePlugin(SiteObject site) : base(site)
    {
        var folder = UPath.Root / ResourceFolderName;
        Providers = new OrderedList<ResourceProvider>()
        {
            new NpmResourceProvider(this)
        };
        Site.SetValue(SiteVariables.Resources, this, true);
        Site.Builtins.Import(SiteVariables.ResourceFunction, (Func<object, string?, object?>)ResourceFunction);
    }

    public OrderedList<ResourceProvider> Providers { get; }

    public ResourceObject? TryLoadResource(string providerName, string packageName, string? packageVersion = null, ResourceInstallFlags flags = 0)
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

    /// <summary>
    /// The `resource` function accessible from scripts.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="version">The version to load.</param>
    /// <returns>A ScriptObject </returns>
    /// <exception cref="LunetException">Unsupported resource parameter found. Supports either a plain string or an object with at least the properties { name: \providerName/packageName[/packageVersion]\ }</exception>
    private object? ResourceFunction(object query, string? version = null)
    {
        var packageFullName = query as string;

        string? providerName = null;
        string? packageName = null;

        var resourceObj = query as ScriptObject;
        var flags = ResourceInstallFlags.Private;
        if (resourceObj != null)
        {
            packageName = resourceObj.GetSafeValue<string>("name");
            providerName = resourceObj.GetSafeValue<string>("provider");
            version = resourceObj.GetSafeValue<string>("version");

            if (resourceObj.GetSafeValue<bool>("public"))
            {
                flags = ResourceInstallFlags.None;
            }

            if (resourceObj.GetSafeValue<bool>("pre_release"))
            {
                flags |= ResourceInstallFlags.PreRelease;
            }
        }
        else if (packageFullName != null)
        {
            ParseQuery(packageFullName, out providerName, out packageName);
        }

        version ??= "latest";

        if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(packageName))
        {
            throw new LunetException($"Unsupported resource parameter found [{query}]. Supports either a plain string or an object with at least the properties like {{ provider: \"npm\", name: \"jquery\" }}");
        }

        var resource = TryLoadResource(providerName, packageName, version, flags);
        return resource;
    }

    private void ParseQuery(string resourceQuery, out string providerName, out string packageName)
    {
        if (resourceQuery == null) throw new ArgumentNullException(nameof(resourceQuery));

        var providerIndex = resourceQuery.IndexOf(':');
        if (providerIndex <= 0)
        {
            throw new LunetException($"Invalid resource name to load [{resourceQuery}]. Expecting a the character ':' between the provider name and package name (e.g: \"npm:jquery\")");
        }

        providerName = resourceQuery.Substring(0, providerIndex);
        packageName = resourceQuery.Substring(providerIndex + 1);
    }
}
