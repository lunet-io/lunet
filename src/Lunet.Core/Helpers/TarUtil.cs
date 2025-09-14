// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Formats.Tar;
using System.IO;
using Zio;

namespace Lunet.Helpers;

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
        using var reader = new TarReader(stream, true);
        while (reader.GetNextEntry() is { } tarEntry)
        {
            if (tarEntry.DataStream is null) continue;
            if (tarEntry.EntryType != TarEntryType.V7RegularFile &&
                tarEntry.EntryType != TarEntryType.RegularFile &&
                tarEntry.EntryType != TarEntryType.ContiguousFile)
            {
                continue;
            }
            
            var entryName = tarEntry.Name;
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
                tarEntry.DataStream!.CopyTo(outputStream);                   
            }
            outputFileEntry.LastWriteTime = tarEntry.ModificationTime.UtcDateTime;
        }
    }

}