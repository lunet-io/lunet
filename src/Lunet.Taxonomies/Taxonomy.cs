// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Taxonomies;

public class TaxonomyTermCollection : DynamicCollection<TaxonomyTerm, TaxonomyTermCollection>
{
    public TaxonomyTermCollection()
    {
    }

    public TaxonomyTermCollection(IEnumerable<TaxonomyTerm> values) : base(values)
    {
    }
}

[DebuggerDisplay("{Name} => {Single} Terms: [{Terms.Count}]")]
public class Taxonomy : DynamicObject<TaxonomyProcessor>
{
    private readonly TaxonomyTermCollection byName;
    private readonly TaxonomyTermCollection byCount;

    public Taxonomy(TaxonomyProcessor parent, string name, string single, string url, ScriptObject map) : base(parent)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (single == null) throw new ArgumentNullException(nameof(single));
        Name = name;
        Url = url ?? $"/{Name}/";
        Map = map ?? new ScriptObject();
        Single = single;
        Terms = new DynamicObject<Taxonomy>(this, StringComparer.OrdinalIgnoreCase);
        byName = new TaxonomyTermCollection();
        byCount = new TaxonomyTermCollection();
        SetValue("name", Name, true);
        SetValue("url", Url, true);
        SetValue("single", Single, true);
        SetValue("map", Map, true);
        SetValue("terms", Terms, true);
        Terms.SetValue("by_name", ByName, true);
        Terms.SetValue("by_count", ByCount, true);
    }

    public string Name { get; }

    public string Url { get; }

    public string Single { get; }

    public ScriptObject Map { get; }

    public DynamicObject Terms { get; }

    public TaxonomyTermCollection ByName => byName;

    public TaxonomyTermCollection ByCount => byCount;

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
        tempByCount.Sort((left, right) => -left.Pages.Count.CompareTo(right.Pages.Count));

        byName.Clear();
        byCount.Clear();
        byName.AddRange(tempByName);
        byCount.AddRange(tempByCount);
    }
}