// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;

namespace Lunet.Taxonomies
{
    public class TaxonomyManager : ManagerBase
    {
        internal TaxonomyManager(SiteObject site) : base(site)
        {
            List = new List<Taxonomy>();
            Declared = new TaxonomiesObject(this);
            Site.SetValue("taxonomies", Declared, true);
            Site.Plugins.Processors.Add(new TaxonomyProcessor());
        }

        public List<Taxonomy> List { get; }

        public TaxonomiesObject Declared { get; }

        public Taxonomy Find(string name)
        {
            foreach (var tax in List)
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

                List.Add(new Taxonomy(this, name, singular));
            }

            // Convert taxonomies to readonly after initialization
            Declared.Clear();
            foreach (var taxonomy in List)
            {
                Declared.SetValue(taxonomy.Name, taxonomy, true);
            }
        }
    }
}