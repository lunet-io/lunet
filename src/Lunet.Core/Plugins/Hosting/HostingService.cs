// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Lunet.Hosting
{
    public class HostingService : ServiceBase
    {
        public const string DefaultBaseUrl = "http://localhost:4000";

        internal HostingService(SiteObject site) : base(site)
        {
            AppBuilders = new OrderedList<Action<IApplicationBuilder>>();

            Site.CommandLine.ServerCommand.Invoke = ServerCommand;
        }

        private int ServerCommand()
        {
            if (Site.BaseUrl == null)
            {
                Site.BaseUrl = DefaultBaseUrl;
            }
            if (Site.BasePath == null)
            {
                Site.BasePath = string.Empty;
            }

            Site.Build();

            var host = new WebHostBuilder()
                .UseLunet(Site)
                .Build();

            host.Run();

            return 0;
        }

        public OrderedList<Action<IApplicationBuilder>> AppBuilders { get; }

        public void Configure(IApplicationBuilder builder)
        {
            // Allow to configure the pipeline
            foreach (var appBuilderAction in AppBuilders)
            {
                appBuilderAction(builder);
            }

            // By default we always serve files at last
            builder.UseFileServer();
        }
    }
}