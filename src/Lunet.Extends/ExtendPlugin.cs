using Lunet.Core;
using Lunet.Extends;
using Lunet.Plugins;

[assembly: SitePlugin(typeof(ExtendPlugin), Order = -100)]

namespace Lunet.Extends
{
    public class ExtendPlugin : SitePlugin
    {
        public override string Name => "resources";

        public override void Initialize(SiteObject site)
        {
            site.Register(new ExtendService(site));
        }
    }
}