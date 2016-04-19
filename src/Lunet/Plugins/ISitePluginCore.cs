using Lunet.Core;

namespace Lunet.Plugins
{
    public interface ISitePluginCore
    {
        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes this plugin with the specified <see cref="SiteObject"/>.
        /// </summary>
        /// <param name="site">The site object</param>
        void Initialize(SiteObject site);
    }
}