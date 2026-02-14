// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;

namespace Lunet.Watcher;

public class BuildCommandRunner : ISiteCommandRunner
{
    public bool Watch { get; set; }
        
    public bool SingleThreaded { get; set; }

    public bool Development { get; set; }

    public async Task<RunnerResult> RunAsync(SiteRunner runner, CancellationToken cancellationToken)
    {
        if (runner.CurrentSite is null)
        {
            return RunnerResult.ExitWithError;
        }

        // Setup the environment
        runner.CurrentSite.Environment = Development ? "dev" : "prod";
        runner.Config.SingleThreaded = SingleThreaded;
        return await RunAsyncImpl(runner, cancellationToken);
    }

    protected virtual async Task<RunnerResult> RunAsyncImpl(SiteRunner runner, CancellationToken cancellationToken)
    {
        if (runner.CurrentSite is null)
        {
            return RunnerResult.ExitWithError;
        }

        runner.CurrentSite.Build();

        if (Watch)
        {
            return await SiteWatcherService.RunAsync(runner, cancellationToken);
        }

        return runner.CurrentSite.HasErrors ? RunnerResult.ExitWithError : RunnerResult.Exit;
    }
}
