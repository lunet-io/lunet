// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Reflection;

namespace Lunet.Core;

/// <summary>
/// Base class for an lunet object that provides a dynamic object
/// accessible from scripts.
/// </summary>
public class LunetObject : DynamicObject<SiteObject>
{
    public LunetObject(SiteObject site) : base(site)
    {
        Version = typeof(LunetObject).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        SetValue("version", Version, true);
    }

    public string Version { get; }
}