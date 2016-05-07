// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lunet.Core;

namespace Lunet.Taxonomies
{
    [DebuggerDisplay("{Name} => {Single} Terms: [{Terms.Count}]")]
    public class Taxonomy : DynamicObject<TaxonomyManager>
    {
        private DynamicCollection<TaxonomyTerm> byName;
        private DynamicCollection<TaxonomyTerm> byCount;

        public Taxonomy(TaxonomyManager parent, string name, string single) : base(parent)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (single == null) throw new ArgumentNullException(nameof(single));
            Name = name;
            Url = $"/{Name}/";
            Single = single;
            Terms = new DynamicObject<Taxonomy>(this);
            SetValue("name", Name, true);
            SetValue("url", Url, true);
            SetValue("single", Single, true);
            SetValue("terms", Terms, true);
        }

        public string Name { get; }

        public string Url { get; }

        public string Single { get; }

        public DynamicObject Terms { get; }

        public IEnumerable<TaxonomyTerm> ByName => byName;

        public IEnumerable<TaxonomyTerm> ByCount => byCount;

        public void AddTerm(TaxonomyTerm term)
        {
            Terms.SetValue(term.Name, term, true);
        }

        internal void Update()
        {
            var tempByName = new List<TaxonomyTerm>();
            var tempByCount = new List<TaxonomyTerm>();
            foreach (var termObj in Terms.Values)
            {
                var term = termObj as TaxonomyTerm;
                if (term == null)
                {
                    continue;
                }

                // Update the TaxonomyTerm
                term.Update();

                tempByName.Add(term);
                tempByCount.Add(term);
            }

            tempByName.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
            tempByCount.Sort((left, right) => left.PageCount.CompareTo(right.PageCount));

            byName = new DynamicCollection<TaxonomyTerm>(tempByName);
            byCount = new DynamicCollection<TaxonomyTerm>(tempByCount);

            Terms.SetValue("by_name", ByName, true);
            Terms.SetValue("by_count", ByCount, true);
        }
    }
}