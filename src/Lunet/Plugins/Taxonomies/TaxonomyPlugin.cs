// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Plugins;
using Lunet.Plugins.Taxonomies;

// Register this plugin
[assembly: SitePlugin(typeof(TaxonomyPlugin))]

namespace Lunet.Plugins.Taxonomies
{
    public class TaxonomyPlugin : SitePlugin
    {
        public override string Name => "taxonomies";

        public override void Initialize(SiteObject site)
        {
            site.Plugins.Processors.Add(new TaxonomyProcessor());
        }
   }
}