// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Scriban.Runtime;

namespace Lunet.Core
{
    /// <summary>
    /// Base implementation of a <see cref="DynamicObject"/> accessible from a script
    /// using scriban <see cref="ScriptObject"/>.
    /// </summary>
    /// <seealso cref="Scriban.Runtime.ScriptObject" />
    /// <seealso cref="IDynamicObject" />
    public class DynamicObject : ScriptObject, IDynamicObject
    {
        public DynamicObject(object parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            Parent = parent;
        }

        public object Parent { get; }
    }

    /// <summary>
    /// Base implementation of a <see cref="DynamicObject"/> accessible from a script
    /// using scriban <see cref="ScriptObject"/>.
    /// </summary>
    /// <seealso cref="Scriban.Runtime.ScriptObject" />
    /// <seealso cref="IDynamicObject" />
    public class DynamicObject<T> : DynamicObject
    {
        public DynamicObject(T parent) : base(parent)
        {
        }
        public new T Parent => (T) base.Parent;
    }
}