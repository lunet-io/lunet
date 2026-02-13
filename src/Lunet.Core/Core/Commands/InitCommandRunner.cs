// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Lunet.Core.Commands;

public class InitCommandRunner : ISiteCommandRunner
{
    public bool Force { get; set; }
        
    public async Task<RunnerResult> RunAsync(SiteRunner runner, CancellationToken cancellationToken)
    {
        return runner.CurrentSite.Create(Force) != 0 ? RunnerResult.ExitWithError : RunnerResult.Exit;
    }
}