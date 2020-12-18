// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Scriban.Runtime;
using Zio.FileSystems;

namespace Lunet.Api
{
    public class ApiModule : SiteModule<ApiPlugin>
    {
    }

    public class ApiPlugin : SitePlugin
    {
        public ApiPlugin(SiteObject site) : base(site)
        {
            site.SetValue("api", this, true);
        }

        public void Register(string name, Func<ApiConfig> apiFunc)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (apiFunc == null) throw new ArgumentNullException(nameof(apiFunc));
            if (ContainsKey(name)) return;
            this.Import(name, apiFunc);
        }
    }
    
    public abstract class ApiConfig: DynamicObject
    {
    }
}