// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using Scriban.Runtime;

namespace Lunet.Core
{
    public class HtmlObject : DynamicObject<ScriptObject>
    {
        public HtmlObject(ScriptObject parent) : base(parent)
        {
            HeadIncludes = new ScriptArray<string>();
            // Import html object
            SetValue("head_includes", HeadIncludes, true);
        }

        public ScriptArray<string> HeadIncludes { get; }
    }
}