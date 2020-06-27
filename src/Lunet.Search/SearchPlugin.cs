// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Bundles;
using Lunet.Core;
using Zio;

namespace Lunet.Search
{
    public class SearchPlugin : SitePlugin
    {
        public static readonly UPath DefaultUrl = UPath.Root / "js" / "lunet-search.db";

        public SearchPlugin(SiteObject site, BundlePlugin bundlePlugin) : base(site)
        {
            BundlePlugin = bundlePlugin;
            Enable = false;
            Url = (string)DefaultUrl;

            Excludes = new PathCollection();
            SetValue("excludes", Excludes, true);

            site.SetValue("search", this, true);

            var processor = new SearchProcessor(this);
            site.Content.BeforeLoadingProcessors.Add(processor);
            // It is important to insert the processor at the beginning 
            // because we output values used by the BundlePlugin
            site.Content.BeforeProcessingProcessors.Insert(0, processor);
            site.Content.AfterLoadingProcessors.Add(processor);
        }

        public bool Enable
        {
            get => GetSafeValue<bool>("enable");
            set => SetValue("enable", value);
        }

        internal BundlePlugin BundlePlugin { get; }

        public PathCollection Excludes { get; }

        public string Url
        {
            get => GetSafeValue<string>("url");
            set => SetValue("url", value);
        }
    }
}