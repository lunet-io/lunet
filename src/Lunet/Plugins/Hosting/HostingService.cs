// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Helpers;
using Microsoft.AspNetCore.Builder;

namespace Lunet.Hosting
{
    public class HostingService : ServiceBase
    {
        internal HostingService(SiteObject site) : base(site)
        {
            AppBuilders = new OrderedList<Action<IApplicationBuilder>>();
        }

        public OrderedList<Action<IApplicationBuilder>> AppBuilders { get; }

        public void Configure(IApplicationBuilder builder)
        {
            foreach (var appBuilderAction in AppBuilders)
            {
                appBuilderAction(builder);
            }
        }
    }
}