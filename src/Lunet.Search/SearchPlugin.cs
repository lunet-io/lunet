// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Zio;

namespace Lunet.Search
{
    public class SearchPlugin : SitePlugin
    {
        public static readonly UPath DefaultUrl = UPath.Root / "js" / "search.db";

        public SearchPlugin(SiteObject site) : base(site)
        {
            Enable = false;
            Url = (string)DefaultUrl;

            Excludes = new PathCollection();
            SetValue("excludes", Excludes, true);

            site.SetValue("search", this, true);

            var processor = new SearchProcessor(this);
            site.Content.BeforeLoadingProcessors.Add(processor);
            site.Content.BeforeProcessingProcessors.Add(processor);
            site.Content.AfterLoadingProcessors.Add(processor);
        }

        public bool Enable
        {
            get => GetSafeValue<bool>("enable");
            set => SetValue("enable", value);
        }

        public PathCollection Excludes { get; }

        public string Url
        {
            get => GetSafeValue<string>("url");
            set => SetValue("url", value);
        }
    }
}