// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System.Diagnostics;
using Lunet.Core;

namespace Lunet.Plugins
{
    /// <summary>
    /// Base class for a plugin.
    /// </summary>
    /// <seealso cref="Lunet.Plugins.ISitePlugin" />
    [DebuggerDisplay("Plugin: {Name}")]
    public abstract class SitePlugin : ISitePlugin
    {
        public abstract void Initialize(SiteObject site);

        public virtual string Name => GetType().Name;
    }
}