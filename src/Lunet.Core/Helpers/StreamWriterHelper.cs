// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Lunet.Helpers;

/// <summary>
/// Helper to write a string to a buffer by keeping the write buffer around
/// </summary>
internal static class StreamWriterHelper
{
    private const int DefaultSize = 16384;

    [ThreadStatic]
    private static byte[] _writeBuffer;

    public static void WriteStringOptimized(this Stream stream, string content, Encoding encoding)
    {
        var byteCount = encoding.GetByteCount(content);

        var buffer = _writeBuffer;
        if (buffer == null)
        {
            buffer = new byte[byteCount < DefaultSize ? DefaultSize : byteCount];
            _writeBuffer = buffer;
        }
        else if (buffer.Length < byteCount)
        {
            byteCount = buffer.Length * 2 >= byteCount ? buffer.Length * 2 : byteCount;
            buffer = new byte[byteCount];
            _writeBuffer = buffer;
        }

        var finalByteCount = encoding.GetBytes(content, buffer);

        stream.Write(buffer, 0, finalByteCount);
    }
}