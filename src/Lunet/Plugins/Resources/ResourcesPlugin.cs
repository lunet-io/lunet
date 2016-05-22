using Lunet.Core;
using Lunet.Plugins;
using Lunet.Resources;

[assembly: SitePlugin(typeof(ResourcesPlugin), Order = -100)]

namespace Lunet.Resources
{
    public class ResourcesPlugin : SitePlugin
    {
        public override string Name => "resources";

        public override void Initialize(SiteObject site)
        {
            site.Register(new ResourceService(site));
        }
    }
}