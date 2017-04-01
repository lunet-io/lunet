using Lunet.Bundles;
using Lunet.Core;
using Lunet.Plugins;
using Lunet.Plugins.Bundles;

[assembly: SitePlugin(typeof(BundlePlugin), Order = -100)]

namespace Lunet.Plugins.Bundles
{
    public class BundlePlugin : SitePlugin
    {
        public override string Name => "bundles";

        public override void Initialize(SiteObject site)
        {
            site.Register(new BundleService(site));
        }
    }
}