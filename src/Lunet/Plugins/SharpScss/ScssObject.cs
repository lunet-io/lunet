// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Plugins.SharpScss
{
    public class ScssObject : DynamicObject<SharpScssPlugin>
    {
        public ScssObject(SharpScssPlugin parent) : base(parent)
        {
            Includes = new ScriptArray();
            SetValue("includes", Includes, true);
        }

        public ScriptArray Includes
        {
            get;
        }
    }
}