using Lunet.Core;

namespace Lunet.Layouts
{
    public class LayoutPlugin : SitePlugin
    {
        public LayoutPlugin(SiteObject site) : base(site)
        {
            Processor = new LayoutProcessor(this);
            site.Content.ContentProcessors.Insert(0, Processor);
        }

        public LayoutProcessor Processor { get; }
    }
}