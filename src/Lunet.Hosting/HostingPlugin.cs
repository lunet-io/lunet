// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using Lunet.Core;
using Lunet.Plugins;
using Lunet.Hosting;

[assembly: SitePlugin(typeof(HostingPlugin))]

namespace Lunet.Hosting
{
    public class HostingPlugin : SitePlugin
    {
        public override string Name => "hosting";

        public override void Initialize(SiteObject site)
        {
            site.Register(new HostingService(site));
        }
    }
}