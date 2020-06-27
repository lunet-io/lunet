// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

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
        public DynamicObject()
        {
        }

        public T GetSafeValue<T>(string name)
        {
            return this[name] is T tvalue ? tvalue : default;
        }
        
        public void SetValue(string name, object value)
        {
            base.SetValue(name, value, false);
        }

        private bool _toStringing = false;

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            // protect against recursive content
            if (_toStringing)
            {
                return "<recurse>";
            }

            _toStringing = true;
            try
            {
                return base.ToString(format, formatProvider);
            }
            finally
            {
                _toStringing = false;
            }
        }
    }

    /// <summary>
    /// Base implementation of a <see cref="DynamicObject"/> accessible from a script
    /// using scriban <see cref="ScriptObject"/>.
    /// </summary>
    /// <seealso cref="Scriban.Runtime.ScriptObject" />
    /// <seealso cref="IDynamicObject" />
    public class DynamicObject<T> : DynamicObject where T : class
    {
        public DynamicObject(T parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            Parent = parent;
        }

        public T Parent { get; }
    }
}