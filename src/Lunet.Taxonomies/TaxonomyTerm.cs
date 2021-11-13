﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using Lunet.Core;
using Markdig.Helpers;

namespace Lunet.Taxonomies;

[DebuggerDisplay("{Name}, Page: {Pages.Count}")]
public class TaxonomyTerm : DynamicObject<Taxonomy>
{
    public TaxonomyTerm(Taxonomy parent, string name) : base(parent)
    {
        Name = name;
        Pages = new PageCollection();
        var urlName = parent.Map.GetSafeValue<string>(name) ?? name;
        Url = $"{parent.Url}{LinkHelper.Urilize(urlName, true)}/";
        SetValue("name", Name, true);
        SetValue("url", Url, true);
        SetValue("pages", Pages, true);
    }

    public string Name { get; }

    public string Url { get; }

    public PageCollection Pages { get; }

    internal void Update()
    {
        // Sort pages by natural order by default
        Pages.Sort();
    }
}