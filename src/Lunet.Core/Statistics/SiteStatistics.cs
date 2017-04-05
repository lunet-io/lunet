// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Core;

namespace Lunet.Statistics
{
    public class SiteStatistics
    {
        public SiteStatistics()
        {
            Content = new Dictionary<ContentObject, ContentStat>();
            Plugins = new Dictionary<ISitePluginCore, PluginStat>();
        }

        public Dictionary<ContentObject, ContentStat> Content { get; }

        public Dictionary<ISitePluginCore, PluginStat> Plugins { get; }

        public TimeSpan TotalTime { get; set; }

        public ContentStat GetContentStat(ContentObject page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            ContentStat stat;
            lock (Content)
            {
                if (!Content.TryGetValue(page, out stat))
                {
                    stat = new ContentStat(page);
                    Content.Add(page, stat);
                }
            }
            return stat;
        }

        public PluginStat GetPluginStat(ISitePluginCore plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            PluginStat stat;
            if (!Plugins.TryGetValue(plugin, out stat))
            {
                stat = new PluginStat(plugin);
                Plugins.Add(plugin, stat);
            }
            return stat;
        }

        public void Dump(Action<string> log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            log($"Total time: {TotalTime.TotalSeconds:0.###}s");

            // TODO: Add report
        }

        public void Reset()
        {
            Plugins.Clear();
            Content.Clear();
            TotalTime = new TimeSpan(0);
        }
    }
}