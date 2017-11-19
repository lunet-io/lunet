// Copyright(c) 2016, Alexandre Mutel
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification
// , are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zio;

// ----------------------------------------------------------------------------
// Latest version of this file is available at: https://github.com/xoofx/NTar
// ----------------------------------------------------------------------------
// This is a single file version to untar a stream.
// Define preprocessor NTAR_PUBLIC to have this class public
// ----------------------------------------------------------------------------
namespace Lunet.Helpers
{
    /// <summary>
    /// An helper class to untar a stream.
    /// </summary>
#if NTAR_PUBLIC
    public
#else
    internal 
#endif
    static class TarUtil
    {
        internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Untars a stream to a specified output directory. 
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="filterPath">The path inside the archive to filter entries from</param>
        /// <exception cref="System.ArgumentNullException">if outputDirectory is null</exception>
        /// <exception cref="InvalidDataException">If an invalid entry was found</exception>
        public static void UntarTo(this Stream stream, DirectoryEntry outputDirectory, string filterPath = null)
        {
            if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));

            if (filterPath != null && !filterPath.EndsWith("/"))
            {
                filterPath = filterPath.Replace("\\", "/") + "/";
            }

            // Untar the stream
            foreach (var entryStream in stream.Untar())
            {
                var entryName = entryStream.FileName;
                if (entryName.StartsWith("./"))
                {
                    entryName = entryName.Substring(2);
                }

                if (filterPath != null)
                {
                    if (entryName.StartsWith(filterPath))
                    {
                        entryName = "./" + entryName.Substring(filterPath.Length);
                    }
                    else
                    {
                        // Skip files that are not part of the sub directory
                        continue;
                    }
                }

                var outputFilePath = UPath.Combine(outputDirectory.FullName, entryName);
                var outputFileEntry = new FileEntry(outputDirectory.FileSystem, outputFilePath);
                var outputDir = outputFileEntry.Directory;
                if (!outputDir.Exists)
                {
                    outputDir.Create();                   
                }
                using (var outputStream = outputFileEntry.Open(FileMode.Create, FileAccess.Write))
                {
                    entryStream.CopyTo(outputStream);                   
                }
                outputFileEntry.LastWriteTime = entryStream.LastModifiedTime;
            }
        }

        /// <summary>
        /// Untars the specified input stream and returns a stream for each file entry. 
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>An enumeration of file entries. The inputstream can be read on each entry with a length of <see cref="TarEntryStream.Length"/></returns>
        /// <exception cref="InvalidDataException">If an invalid entry was found</exception>
        public static IEnumerable<TarEntryStream> Untar(this Stream inputStream)
        {
            var header = new byte[512];

            long position = 0;

            while (true)
            {
                int zeroBlockCount = 0;
                while (true)
                {
                    // Read the 512 byte block header
                    int length = inputStream.Read(header, 0, header.Length);
                    if (length < 512)
                    {
                        throw new InvalidDataException($"Invalid header block size < 512");
                    }
                    position += length;

                    // Check if the block is full of zero
                    bool isZero = true;
                    for (int i = 0; i < header.Length; i++)
                    {
                        if (header[i] != 0)
                        {
                            isZero = false;
                            break;
                        }
                    }

                    if (isZero)
                    {
                        // If it is full of zero two consecutive times, we have to exit
                        zeroBlockCount++;
                        if (zeroBlockCount == 1)
                        {
                            yield break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // Read file name
                var fileName = GetString(header, 0, 100);

                // read checksum
                var checksum = ReadOctal(header, 148, 8);
                if (!checksum.HasValue)
                {
                    throw new InvalidDataException($"Invalid checksum for file entry [{fileName}] ");
                }

                // verify checksum
                uint checksumVerif = 0;
                for (int i = 0; i < header.Length; i++)
                {
                    var c = header[i];
                    if (i >= 148 && i < (148 + 8))
                    {
                        c = 32;
                    }
                    checksumVerif += c;
                }

                // Checksum is invalid, exit
                if (checksum.Value != checksumVerif)
                {
                    throw new InvalidDataException($"Invalid checksum verification for file entry [{fileName}] ");
                }

                // Read file size
                var fileSizeRead = ReadOctal(header, 124, 12);
                if (!fileSizeRead.HasValue)
                {
                    throw new InvalidDataException($"Invalid filesize for file entry [{fileName}] ");
                }

                var fileLength = fileSizeRead.Value;


                // Read the type of the file entry
                var type = header[156];
                // We support only the File type
                TarEntryStream tarEntryStream = null;
                if (type == '0')
                {
                    // Read timestamp
                    var unixTimeStamp = ReadOctal(header, 136, 12);
                    if (!unixTimeStamp.HasValue)
                    {
                        throw new InvalidDataException($"Invalid timestamp for file entry [{fileName}] ");
                    }
                    var lastModifiedTime = Epoch.AddSeconds(unixTimeStamp.Value).ToLocalTime();

                    // Double check magic ustar to load prefix filename
                    var ustar = GetString(header, 257, 8);
                    // Check for ustar only
                    if (ustar != null && ustar.Trim() == "ustar")
                    {

                        var prefixFileName = GetString(header, 345, 155);
                        fileName = prefixFileName + fileName;
                    }

                    tarEntryStream = new TarEntryStream(inputStream, position, fileLength)
                    {
                        FileName = fileName,
                        LastModifiedTime = lastModifiedTime
                    };

                    // Wrap the region into a slice of the original stream
                    yield return tarEntryStream;
                }

                // The end of the file entry is aligned on 512 bytes
                var untilPosition = (position + fileLength + 511) & ~511;
                if (tarEntryStream != null)
                {
                    position += tarEntryStream.Position;
                }

                // We seek to untilPosition by reading the remaining bytes
                // as we don't want to rely on stream.Seek/Position as it is
                // not working with GzipStream for example
                int delta;
                while ((delta = (int)(untilPosition - position)) > 0)
                {
                    delta = Math.Min(512, delta);
                    var readCount = inputStream.Read(header, 0, delta);
                    position += readCount;
                    if (readCount == 0)
                    {
                        break;
                    }
                }

                // If we are not at target position, there is an error, so exit
                if ((untilPosition - position) != 0)
                {
                    throw new InvalidDataException($"Invalid end of entry after file entry [{fileName}] ");
                }
            }
        }

        /// <summary>
        /// Gets an ASCII string ending by a `\0`
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <returns>A string</returns>
        private static string GetString(byte[] buffer, int index, int count)
        {
            var text = new StringBuilder();
            for (int i = index; i < index + count; i++)
            {
                if (buffer[i] == 0 || buffer[i] >= 127)
                {
                    break;
                }

                text.Append((char)buffer[i]);
            }
            return text.ToString();
        }

        /// <summary>
        /// Reads an octal number converted to integer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <returns>An octal number converted to a long; otherwise <c>null</c> if the conversion failed</returns>
        private static long? ReadOctal(byte[] buffer, int index, int count)
        {
            long value = 0;
            for (int i = index; i < index + count; i++)
            {
                var c = buffer[i];
                if (c == 0)
                {
                    break;
                }
                if (c == ' ')
                {
                    continue;
                }
                if (c < '0' || c > '7')
                {
                    return null;
                }
                value = (value << 3) + (c - '0');
            }
            return value;
        }

        /// <summary>
        /// An Tar entry stream for a file entry from a tar stream.
        /// </summary>
        /// <seealso cref="System.IO.Stream" />
        public class TarEntryStream : Stream
        {
            private readonly Stream stream;
            private readonly long start;
            private long position;

            /// <summary>
            /// Initializes a new instance of the <see cref="TarEntryStream"/> class.
            /// </summary>
            /// <param name="stream">The stream.</param>
            /// <param name="start">The start.</param>
            /// <param name="length">The length.</param>
            /// <exception cref="System.ArgumentNullException"></exception>
            public TarEntryStream(Stream stream, long start, long length)
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));
                this.stream = stream;
                this.start = start;
                position = start;
                this.Length = length;
            }

            /// <summary>
            /// Gets the name of the file entry.
            /// </summary>
            public string FileName { get; internal set; }

            /// <summary>
            /// Gets the timestamp of the file entry.
            /// </summary>
            public DateTime LastModifiedTime { get; internal set; }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count == 0) return 0;

                var maxCount = (int)Math.Min(count, start + Length - position);
                var readCount = stream.Read(buffer, offset, maxCount);
                position += readCount;
                return readCount;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;

            public override long Length { get; }

            public override long Position
            {
                get { return position - start; }
                set
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
