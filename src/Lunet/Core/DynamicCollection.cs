// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Scriban.Runtime;

namespace Lunet.Core
{
    public class DynamicCollection<T> : ScriptArray<T> where T : class
    {
        protected delegate DynamicCollection<T> OrderDelegate();

        private delegate IEnumerable<PageCollection> GroupByDelegate(string key);

        public DynamicCollection()
        {
            InitializeBuiltins();
        }

        public DynamicCollection(IEnumerable<T> values) : base(values)
        {
            InitializeBuiltins();
        }

        public DynamicCollection<T> Reverse()
        {
            return new DynamicCollection<T>(((IEnumerable<T>)this).Reverse());
        }

        private void InitializeBuiltins()
        {
            this.Import("reverse", (OrderDelegate)Reverse);
        }
    }
}