// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Scss
{
    public class ScssObject : DynamicObject<ScssPlugin>
    {
        public ScssObject(ScssPlugin parent) : base(parent)
        {
            Includes = new ScriptArray();
            SetValue("includes", Includes, true);
            this.Import("add_include", (Action<string>)AddInclude);
        }

        public ScriptArray Includes
        {
            get;
        }
        
        public void AddInclude(string path)
        {
            Includes.Add(path);
        }
    }
}