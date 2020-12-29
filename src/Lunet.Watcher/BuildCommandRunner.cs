// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading;
using Lunet.Core;

namespace Lunet.Watcher
{
    public class BuildCommandRunner : ISiteCommandRunner
    {
        public bool Watch { get; set; }
        
        public bool SingleThreaded { get; set; }

        public bool Development { get; set; }

        public RunnerResult Run(SiteRunner runner, CancellationToken cancellationToken)
        {
            // Setup the environment
            runner.CurrentSite.Environment = Development ? "dev" : "prod";
            runner.Config.SingleThreaded = SingleThreaded;
            return RunImpl(runner, cancellationToken);
        }

        protected virtual RunnerResult RunImpl(SiteRunner runner, CancellationToken cancellationToken)
        {
            runner.CurrentSite.Build();

            if (Watch)
            {
                return SiteWatcherService.Run(runner, cancellationToken);
            }

            return runner.CurrentSite.HasErrors ? RunnerResult.ExitWithError : RunnerResult.Exit;
        }
    }
}