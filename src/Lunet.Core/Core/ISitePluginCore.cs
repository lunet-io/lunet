using System;
using System.Diagnostics;

namespace Lunet.Core
{
    public interface ISitePluginCore
    {
        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the site used by this plugin
        /// </summary>
        SiteObject Site { get; }
    }

    [DebuggerDisplay("Plugin: {Name}")]
    public abstract class SitePluginCore : DynamicObject, ISitePluginCore
    {
        protected SitePluginCore(SiteObject site)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));

        }

        public virtual string Name => GetType().Name;

        public SiteObject Site { get; }
    }
}