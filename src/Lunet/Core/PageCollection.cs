// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Scriban.Runtime;

namespace Lunet.Core
{
    public class PageCollection : DynamicCollection<ContentObject>
    {
        //private delegate PageCollection OrderDelegate();

        private delegate IEnumerable<PageCollection> GroupByDelegate(string key);

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
            return new PageCollection(this.OrderBy(o => o.Weight).ThenBy(o => o.Date));
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

        public new PageCollection Reverse()
        {
            return new PageCollection(((IEnumerable<ContentObject>)this).Reverse());
        }

        public IEnumerable<PageCollection> GroupBy(string key)
        {
            // Query object in natural order
            foreach (var group in this.OrderBy(o => o.Weight).ThenBy(o => o.Date).GroupBy(obj => obj[key], o => o))
            {
                var groupCollection = new PageCollection(group);
                groupCollection.SetValue("key", key, true);
                yield return groupCollection;
            }
        }

        /// <summary>
        /// Sorts this instance to the natural order (by weigth, and then by date)
        /// </summary>
        public void Sort()
        {
            var items = this.OrderByWeight();
            this.Clear();
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        private void InitializeBuiltins()
        {
            this.Import("by_weight", (OrderDelegate) OrderByWeight);
            this.Import("by_date", (OrderDelegate)OrderByDate);
            this.Import("by_length", (OrderDelegate)OrderByLength);
            this.Import("by_title", (OrderDelegate)OrderByTitle);
            this.Import("reverse", (OrderDelegate)Reverse);
            this.Import("group_by", (GroupByDelegate) GroupBy);
        }
    }
}