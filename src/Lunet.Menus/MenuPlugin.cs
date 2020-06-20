using Lunet.Core;

namespace Lunet.Menus
{
    public class MenuPlugin : SitePlugin
    {
        public MenuPlugin(SiteObject site) : base(site)
        {
            Processor = new MenuProcessor(this);
            Site.SetValue("menu", this, true);
            Site.Content.BeforeLoadingProcessors.Insert(0, Processor);
        }

        public MenuProcessor Processor { get; }
    }
}