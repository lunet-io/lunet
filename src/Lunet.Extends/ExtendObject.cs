// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Zio;

namespace Lunet.Extends;

public sealed class ExtendObject : DynamicObject
{
    internal ExtendObject(SiteObject site, string fullName, string name, string? version, string? description, string? url, IFileSystem fileSystem)
    {
        Site = site ?? throw new ArgumentNullException(nameof(site));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        Version = version;
        Description = description;
        Url = url;

        SetValue("name", Name, true);
        SetValue("version", Version, true);
        SetValue("description", Description, true);
        SetValue("url", Url, true);
    }
    public SiteObject Site { get; }

    public IFileSystem FileSystem { get; }

    public string Name { get; }

    public string FullName { get; }

    public string? Version { get; }

    public string? Description { get; }

    public string? Url { get; }
}
