// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Scriban.Runtime;

namespace Lunet.Core
{
    public class PageCollection : DynamicCollection<ContentObject, PageCollection>
    {
        public PageCollection()
        {
            InitializeBuiltins();
        }

        public PageCollection(IEnumerable<ContentObject> values) : base(values)
        {
            InitializeBuiltins();
        }

        public PageCollection OrderByWeight()
        {
            return new PageCollection(this.OrderBy(o => o.Weight).ThenByDescending(o => o.Date));
        }

        public PageCollection OrderByDate()
        {
            return new PageCollection(this.OrderBy(o => o.Date));
        }

        public PageCollection OrderByLength()
        {
            return new PageCollection(this.OrderBy(o => o.Length));
        }

        public PageCollection OrderByTitle()
        {
            return new PageCollection(this.OrderBy(o => o.Title));
        }

        protected override IEnumerable<ContentObject> OrderByDefault()
        {
            return OrderByWeight();
        }

        /// <summary>
        /// Sorts this instance to the natural order (by weigth, and then by date)
        /// </summary>
        public void Sort()
        {
            var items = this.OrderByDefault();
            this.Clear();
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        private void InitializeBuiltins()
        {
            this.Import("by_weight", (OrderDelegate)OrderByWeight);
            this.Import("by_date", (OrderDelegate)OrderByDate);
            this.Import("by_length", (OrderDelegate)OrderByLength);
            this.Import("by_title", (OrderDelegate)OrderByTitle);
        }
    }
}