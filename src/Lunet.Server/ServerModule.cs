// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Helpers;
using XenoAtom.CommandLine;

namespace Lunet.Server;

public class ServerModule : SiteModule<ServerPlugin>
{
    protected override void Configure(SiteApplication application)
    {
        base.Configure(application);

        // Adds the server command
        ServerCommand = application.AddCommand("serve", "Builds the website, runs a web server and watches for changes", newApp =>
        {
            var noWatchOption = false;
            var singleThreadedOption = false;
            newApp.Add(new HelpOption("h|help"));
            newApp.Add("no-watch", "Disables watching files and triggering of a new run", _ => noWatchOption = true);
            newApp.Add("no-threads", "Disables multi-threading", _ => singleThreadedOption = true);

            newApp.Add((_, _) =>
            {
                var command = application.CreateCommandRunner<ServeCommandRunner>();
                command.Watch = !noWatchOption;
                command.SingleThreaded = singleThreadedOption;
                return ValueTask.FromResult(0);
            });
        });
    }

    public Command ServerCommand { get; private set; }
}
