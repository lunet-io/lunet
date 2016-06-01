// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
namespace Lunet.Core
{
    /// <summary>
    /// Base interface for a dynamic object accessible from a script.
    /// </summary>
    public interface IDynamicObject
    {
        T GetSafeValue<T>(string name);

        object this[string name] { get; set; }

        void SetValue(string name, object value, bool readOnly);

        bool Remove(string name);
    }
}