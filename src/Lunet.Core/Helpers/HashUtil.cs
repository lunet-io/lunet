// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Lunet.Helpers;

internal class HashUtil
{
    public static unsafe UInt128 HashString(string content)
    {
        return XxHash128.HashToUInt128(MemoryMarshal.Cast<char, byte>(content.AsSpan()));
    }
}
