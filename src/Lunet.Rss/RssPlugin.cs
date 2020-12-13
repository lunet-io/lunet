// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Lunet.Layouts;
using Zio;

namespace Lunet.Rss
{
    public class RssPlugin : SitePlugin
    {
        public RssPlugin(SiteObject site, LayoutPlugin layoutPlugin) : base(site)
        {
            Site.Content.OrderLayoutTypes.Add("rss");
            layoutPlugin.Processor.RegisterLayoutPathProvider("rss", RssLayout);

            site.SetValue("rss", this, true);
            Limit = 10;
        }

        public int Limit
        {
            get => GetSafeValue<int>("limit");
            set => SetValue("limit", value, false);
        }
        
        private static IEnumerable<UPath> RssLayout(SiteObject site, string layoutName, string layoutType)
        {
            // try: .lunet/layouts/{layoutName}/rss.{layoutExtension}
            yield return (UPath)layoutName / (layoutType);

            // try: .lunet/layouts/{layoutName}.rss.{layoutExtension}
            yield return (UPath)(layoutName + "." + layoutType);

            if (layoutName != LayoutProcessor.DefaultLayoutName)
            {
                // try: .lunet/layouts/_default/rss.{layoutExtension}
                yield return (UPath)LayoutProcessor.DefaultLayoutName / (layoutType);

                // try: .lunet/layouts/_default.rss.{layoutExtension}
                yield return (UPath)(LayoutProcessor.DefaultLayoutName + "." + layoutType);
            }
        }
    }
}
