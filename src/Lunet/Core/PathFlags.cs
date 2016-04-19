// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;

namespace Lunet.Core
{
    [Flags]
    public enum PathFlags
    {
        File = 0,

        Directory = 1,

        Normalize = 2
    }

    public static class PathFlagsExtension
    {
        public static bool Normalize(this PathFlags flags)
        {
            return (flags & PathFlags.Normalize) != 0;
        }
        public static bool IsDirectory(this PathFlags flags)
        {
            return (flags & PathFlags.Directory) != 0;
        }
    }
}