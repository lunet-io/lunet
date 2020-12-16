// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading;

namespace Lunet.Core.Commands
{
    public class CleanCommandRunner : ISiteCommandRunner
    {
        public RunnerResult Run(SiteRunner runner, CancellationToken cancellationToken)
        {
            return runner.CurrentSite.Clean() != 0 ? RunnerResult.ExitWithError : RunnerResult.Exit;
        }
    }
}