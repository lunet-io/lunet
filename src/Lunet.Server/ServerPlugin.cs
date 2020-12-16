// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Server
{
    public class ServerPlugin : SitePlugin
    {
        public ServerPlugin(SiteObject site) : base(site)
        {
            Site.Scripts.SiteFunctions.LogObject["server"] = true;
            // Setup by default livereload
            Site.SetLiveReload(true);
        }
    }
}