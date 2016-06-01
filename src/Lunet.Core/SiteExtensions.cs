// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace Lunet
{
    public static class SiteExtensions
    {
        public static IWebHostBuilder UseLunet(this IWebHostBuilder hostBuilder, SiteObject site)
        {
            hostBuilder
                .UseKestrel()
                .UseContentRoot(site.OutputDirectory)
                .UseWebRoot(site.OutputDirectory)
                .UseUrls(site.BaseUrl);

            var hostingService = site.GetService<HostingService>();
            if (hostingService != null)
            {
                hostBuilder.Configure(site.GetService<HostingService>().Configure);
            }

            return hostBuilder;
        }
    }
}