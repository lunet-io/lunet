// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Threading;
using Lunet.Helpers;

namespace Lunet.Core.Commands
{
    public class InitCommandRunner : ISiteCommandRunner
    {
        public bool Force { get; set; }
        
        public RunnerResult Run(SiteRunner runner, CancellationToken cancellationToken)
        {
            return runner.CurrentSite.Create(true) != 0 ? RunnerResult.ExitWithError : RunnerResult.Exit;
        }
    }
}