// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Helpers
{
    internal class HashUtil
    {
        public static unsafe void Blake3HashString(string content, out Blake3.Hash hash)
        {
            fixed (void* pData = content)
            {
                hash = Blake3.Hasher.Hash(new ReadOnlySpan<byte>(pData, content.Length * 2));
            }
        }
    }
}