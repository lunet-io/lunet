// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Taxonomies
{
    public class TaxonomyManager : ManagerBase
    {
        internal TaxonomyManager(SiteObject site) : base(site)
        {
            Computed = new List<Taxonomy>();
            Declared = new DynamicObject<TaxonomyManager>(this);
            Site.DynamicObject.SetValue("taxonomies", Declared, true);
            Site.Plugins.Processors.Add(new TaxonomyProcessor());
        }

        public List<Taxonomy> Computed { get; }

        public DynamicObject<TaxonomyManager> Declared { get; }

        public Taxonomy Find(string name)
        {
            foreach (var tax in Computed)
            {
                if (tax.Name == name)
                {
                    return tax;
                }
            }
            return null;
        }

        public override void InitializeAfterConfig()
        {
            foreach (var taxonomy in Declared)
            {
                var name = taxonomy.Key;
                var singular = taxonomy.Value as string;
                if (string.IsNullOrWhiteSpace(singular))
                {
                    Site.Error($"Invalid singular form [{singular}] of taxonomy [{name}]. Expecting a string");
                    continue;
                }
                // TODO: verify that plural is a valid identifier

                var tax = Find(name);
                if (tax != null)
                {
                    continue;
                }

                Computed.Add(new Taxonomy(this, name, singular));
            }
        }
    }
}