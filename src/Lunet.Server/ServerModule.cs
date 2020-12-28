// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Helpers;

namespace Lunet.Server
{
    public class ServerModule : SiteModule<ServerPlugin>
    {
        protected override void Configure(SiteApplication application)
        {
            base.Configure(application);

            // Adds the server command
            ServerCommand = application.Command("serve", newApp =>
            {
                newApp.Description = "Builds the website, runs a web server and watches for changes";
                newApp.HelpOption("-h|--help");
                var noWatchOption = newApp.Option("--no-watch", "Disables watching files and triggering of a new run", CommandOptionType.NoValue);
                var singleThreadedOption = newApp.Option("--no-threads", "Disables multi-threading", CommandOptionType.NoValue);

                newApp.Invoke = () =>
                {
                    var command = application.CreateCommandRunner<ServeCommandRunner>();
                    command.Watch = !noWatchOption.HasValue();
                    command.SingleThreaded = singleThreadedOption.HasValue();
                };
            });
        }

        public CommandLineApplication ServerCommand { get; private set; }
    }
}