using System;
using Lunet.Plugins;

namespace Lunet.Statistics
{
    public class PluginStat
    {
        public PluginStat(ISitePluginCore plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            Plugin = plugin;
        }

        public ISitePluginCore Plugin { get; }

        public int Order { get; set; }

        public int PageCount { get; set; }

        public TimeSpan InitDuration { get; set; }

        public TimeSpan BeginDuration { get; set; }

        public TimeSpan PageDuration { get; set; }

        public TimeSpan EndDuration { get; set; }
    }
}