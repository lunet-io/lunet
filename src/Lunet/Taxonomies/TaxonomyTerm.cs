// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics;
using Lunet.Core;
using Markdig.Helpers;

namespace Lunet.Taxonomies
{
    [DebuggerDisplay("{Name}, Page: {PageCount}")]
    public class TaxonomyTerm : DynamicObject<Taxonomy>
    {
        public TaxonomyTerm(Taxonomy parent, string name) : base(parent)
        {
            Name = name;
            Pages = new List<ContentObject>();
            Url = $"{parent.Url}{LinkHelper.Urilize(name, true)}/";

            SetValue("name", Name, true);
            SetValue("url", Url, true);
            SetValue("count", 0, true);
            SetValue("pages", Pages, true);
        }

        public string Name { get; }

        public string Url { get; }

        public int PageCount => Pages.Count;

        public List<ContentObject> Pages { get; }

        internal void Update()
        {
            SetValue("count", PageCount, true);
        }
    }
}