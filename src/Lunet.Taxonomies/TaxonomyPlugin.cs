// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Layouts;

// Register this plugin

namespace Lunet.Taxonomies
{

    public class TaxonomyModule : SiteModule<TaxonomyPlugin>
    {
    }


    public class TaxonomyPlugin : SitePlugin
    {
        public TaxonomyPlugin(SiteObject site, LayoutPlugin layoutPlugin) : base(site)
        {
            if (layoutPlugin == null) throw new ArgumentNullException(nameof(layoutPlugin));
            site.Content.BeforeProcessingProcessors.Add(new TaxonomyProcessor(this, layoutPlugin));
        }
    }
}