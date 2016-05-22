// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Resources
{
    [Flags]
    public enum ResourceInstallFlags
    {
        None = 0,

        Private = 1,

        PreRelease = 2,

        NoInstall = 4,
    }
}