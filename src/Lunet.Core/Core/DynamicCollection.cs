// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Scriban.Runtime;

namespace Lunet.Core;

public abstract class DynamicCollection<T, TInstance> : ScriptArray<T> where T : IScriptObject where TInstance: DynamicCollection<T, TInstance>, new()
{
    private delegate int CountDelegate();

    protected delegate TInstance OrderDelegate();

    protected delegate IEnumerable<TInstance> GroupByDelegate(string key);

    protected DynamicCollection()
    {
        InitializeBuiltins();
    }

    protected DynamicCollection(IEnumerable<T> values) : base(values)
    {
        InitializeBuiltins();
    }

    public TInstance Reverse()
    {
        var instance = new TInstance();
        foreach (var item in ((IEnumerable<T>) this).Reverse())
        {
            instance.Add(item);
        }
        return instance;
    }

    protected virtual IEnumerable<T> OrderByDefault()
    {
        return this;
    }

    public virtual IEnumerable<TInstance> GroupBy(string key)
    {
        // Query object in natural order
        foreach (var group in OrderByDefault().GroupBy(obj =>
                 {
                     obj.TryGetValue(key, out var value);
                     return value;
                 }, o => o))
        {
            var groupCollection = Clone();
            foreach (var item in group)
            {
                groupCollection.Add(item);
            }
            groupCollection.SetValue("key", key, true);
            yield return groupCollection;
        }
    }

    protected virtual TInstance Clone()
    {
        return new TInstance();
    }
        
    public TItem? GetSafeValue<TItem>(string name)
    {
        return this.ScriptObject[name] is TItem tvalue ? tvalue : default;
    }

    public void SetValue(string name, object value, bool isReadOnly = false)
    {
        ScriptObject.SetValue(name, value, isReadOnly);
    }

    private void InitializeBuiltins()
    {
        this.Import("count", (CountDelegate)(() => Count));
        this.Import("reverse", (OrderDelegate)Reverse);
        this.Import("group_by", (GroupByDelegate)GroupBy);
    }
}
