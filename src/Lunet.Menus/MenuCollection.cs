// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using Lunet.Core;

namespace Lunet.Menus;

[DebuggerDisplay("Count = {Count,nq}")]
[DebuggerTypeProxy(typeof(DebuggerProxy))]
public class MenuCollection : DynamicCollection<MenuObject, MenuCollection>
{
    private class DebuggerProxy
    {
        private readonly MenuCollection _collection;

        public DebuggerProxy(MenuCollection collection)
        {
            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MenuObject[] Items => _collection.ToArray();
    }
}