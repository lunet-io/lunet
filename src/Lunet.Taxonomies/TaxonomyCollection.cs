// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Taxonomies
{
    public class TaxonomyCollection : DynamicCollection<Taxonomy, TaxonomyCollection>
    {
        public TaxonomyCollection()
        {
            this.Import("clear", (Action)ClearDefinitions);
        }

        private void ClearDefinitions()
        {
            ScriptObject.Clear();
        }
    }
}