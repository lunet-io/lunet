// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
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
    public class DynamicObject : ScriptObject
    {
        public DynamicObject()
        {
        }

        public DynamicObject(IEqualityComparer<string> keyComparer) : base(keyComparer)
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

        public DynamicObject(T parent, IEqualityComparer<string> keyComparer) : base(keyComparer)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            Parent = parent;
        }

        public T Parent { get; }
    }

    public static class DynamicObjectExtensions
    {
        public static T GetSafeValue<T>(this IScriptObject obj, string name)
        {
            return obj.TryGetValue(name, out var value) && value is T tvalue ? tvalue : default;
        }

        public static T GetSafeValueFromPageOrSite<T>(this TemplateObject obj, string name, T defaultValue = default)
        {
            if (!obj.TryGetValue(name, out var value))
            {
                if (!obj.TryGetValue(name, out value))
                {
                    return defaultValue;
                }
            } 
            return value is T rvalue ? rvalue : defaultValue;
        }

        public static void CopyToWithReadOnly(this ScriptObject from, ScriptObject to)
        {
            if (@from == null) throw new ArgumentNullException(nameof(@from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (ReferenceEquals(from, to)) return;

            foreach (var (key, value) in from)
            {
                var isReadOnly = !from.CanWrite(key);
                to.SetValue(key, value, isReadOnly);
            }
        }
    }
}