// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Threading;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Watcher;

namespace Lunet.Server
{
    public class ServerCommandRunner : ISiteCommandRunner
    {
        public ServerCommandRunner()
        {
            EnableWatch = true;
        }
        
        public bool EnableWatch { get; set; }

        public RunnerResult Run(SiteRunner runner, CancellationToken cancellationToken)
        {
            var site = runner.CurrentSite;
            var runnerResult = RunnerResult.Exit;
            try
            {
                // Start or Update the web server
                var serverService = runner.GetService<SiteServerService>();
                if (serverService == null)
                {
                    serverService = new SiteServerService(runner.Config);
                    runner.RegisterService(serverService);
                }

                // Make BaseUrl to 
                if (site.BaseUrl == null || !site.BaseUrlForce)
                {
                    site.BaseUrl = SiteServerService.DefaultBaseUrl;
                }
                if (site.BasePath == null || !site.BaseUrlForce)
                {
                    site.BasePath = string.Empty;
                }

                // Initialize the website
                bool needRestart = false;
                if (site.Initialize())
                {
                    needRestart = serverService.Update(site);

                    if (serverService.LiveReload)
                    {
                        SiteServerService.SetupLiveReloadClient(site);
                    }
                }

                // Build the files
                site.Build();
                
                // Start or restart the web server
                if (needRestart)
                {
                    // Run the web server if necessary
                    serverService.StartOrUpdate(cancellationToken);
                }
                else if (serverService.LiveReload)
                {
                    serverService.NotifyReloadToClients();
                }

                // Watch events
                if (EnableWatch)
                {
                    runnerResult = SiteWatcherService.Run(runner, cancellationToken);

                    // Wait also for the termination of the web server
                    if (cancellationToken.IsCancellationRequested)
                    {
                        serverService.ShutdownAndWaitForShutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                runner.Config.Error($"Unexpected error while reloading the site. Reason: {ex.GetReason()}");
                runnerResult = RunnerResult.ExitWithError;
            }

            return runnerResult;
        }
    }
}