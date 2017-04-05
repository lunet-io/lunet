// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System.Diagnostics;

namespace Lunet.Core
{
    /// <summary>
    /// Base class for a plugin.
    /// </summary>
    /// <seealso cref="Lunet.Core.ISitePlugin" />
    [DebuggerDisplay("Plugin: {Name}")]
    public abstract class SitePlugin : SitePluginCore, ISitePlugin
    {
        protected SitePlugin(SiteObject site) : base(site)
        {
        }
    }
}