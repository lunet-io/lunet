using Lunet.Core;
using Lunet.Layouts;
using Lunet.Plugins;
using Lunet.Plugins.Layouts;

[assembly: SitePlugin(typeof(LayoutPlugin))]

namespace Lunet.Plugins.Layouts
{
    public class LayoutPlugin : SitePlugin
    {
        public override string Name => "layouts";

        public override void Initialize(SiteObject site)
        {
            site.Generator.Processors.Insert(0, new LayoutProcessor());
        }
    }
}