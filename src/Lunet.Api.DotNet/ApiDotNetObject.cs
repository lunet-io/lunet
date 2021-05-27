// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Api.DotNet
{
    public class ApiDotNetObject : ScriptObject
    {
        public ApiDotNetObject()
        {
            Namespaces = new ScriptArray<ScriptObject>();
            Objects = new ScriptObject();
            References = new ScriptObject();
        }

        public new void Clear()
        {
            Namespaces.Clear();
            Objects.Clear();
            References.Clear();
        }

        public ScriptArray<ScriptObject> Namespaces
        {
            get => this.GetSafeValue<ScriptArray<ScriptObject>>("namespaces");
            private init => this.SetValue("namespaces", value, true);
        }

        public ScriptObject Objects
        {
            get => this.GetSafeValue<ScriptObject>("objects");
            private init => this.SetValue("objects", value, true);
        }

        public ScriptObject References
        {
            get => this.GetSafeValue<ScriptObject>("references");
            private init => this.SetValue("references", value, true);
        }
    }
}