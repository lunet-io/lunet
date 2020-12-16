using Lunet.Core;

namespace Lunet.Menus
{
    public class MenuModule : SiteModule<MenuPlugin>
    {
    }

    public class MenuPlugin : SitePlugin
    {
        public MenuPlugin(SiteObject site) : base(site)
        {
            Processor = new MenuProcessor(this);
            Site.SetValue("menu", this, true);
            Site.Content.AfterRunningProcessors.Insert(0, Processor);
            Site.Content.BeforeProcessingProcessors.Insert(0, Processor);
        }

        public MenuProcessor Processor { get; }
    }
}