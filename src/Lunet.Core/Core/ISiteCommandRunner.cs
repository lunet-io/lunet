// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading;

namespace Lunet.Core;

public interface ISiteCommandRunner
{
    RunnerResult Run(SiteRunner runner, CancellationToken cancellationToken);
}


public enum RunnerResult
{
    Exit,
    Continue,
    ExitWithError,
}