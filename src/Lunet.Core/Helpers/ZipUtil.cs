// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.IO.Compression;
using Zio;

namespace Lunet.Helpers;

public static class ZipUtil
{
    public static void ExtractToDirectory(this ZipArchive source, DirectoryEntry outputDirectory, string filterPath = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (outputDirectory == null)
            throw new ArgumentNullException(nameof(outputDirectory));

        // Rely on Directory.CreateDirectory for validation of destinationDirectoryName.

        // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }
        if (filterPath != null && !filterPath.EndsWith("/"))
        {
            filterPath = filterPath.Replace("\\", "/") + "/";
        }

        foreach (ZipArchiveEntry entry in source.Entries)
        {
            var entryName = entry.FullName;
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

            var destinationPath = UPath.Combine(outputDirectory.Path, entryName);
            if (entryName.EndsWith("/") || entryName.EndsWith("\\"))
            {
                // If it is a directory:

                if (entry.Length != 0)
                    throw new IOException("Zip entry name ends in directory separator character but contains data");

                var destinationDir = new DirectoryEntry(outputDirectory.FileSystem, destinationPath);
                if (!destinationDir.Exists)
                {
                    destinationDir.Create();
                }
            }
            else
            {
                // If it is a file:
                // Create containing directory:
                var destinationFile = new FileEntry(outputDirectory.FileSystem, destinationPath);
                var destinationDir = destinationFile.Directory;
                if (!destinationDir.Exists)
                {
                    destinationDir.Create();
                }
                entry.ExtractToFile(destinationFile, true);
            }
        }
    }

    /// <summary>
    /// Creates a file on the file system with the entry?s contents and the specified name. The last write time of the file is set to the
    /// entry?s last write time. This method does not allow overwriting of an existing file with the same name. Attempting to extract explicit
    /// directories (entries with names that end in directory separator characters) will not result in the creation of a directory.
    /// </summary>
    /// 
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only white space, or contains one or more
    /// invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
    /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
    /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
    /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid (for example, it is on
    /// an unmapped drive).</exception>
    /// <exception cref="IOException">destinationFileName already exists.
    /// -or- An I/O error has occurred. -or- The entry is currently open for writing.
    /// -or- The entry has been deleted from the archive.</exception>
    /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
    /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
    /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
    /// -or- The entry has been compressed using a compression method that is not supported.</exception>
    /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
    /// 
    /// <param name="destinationFile">The name of the file that will hold the contents of the entry.
    /// The path is permitted to specify relative or absolute path information.
    /// Relative path information is interpreted as relative to the current working directory.</param>
    public static void ExtractToFile(this ZipArchiveEntry source, FileEntry destinationFile)
    {
        ExtractToFile(source, destinationFile, false);
    }


    /// <summary>
    /// Creates a file on the file system with the entry?s contents and the specified name.
    /// The last write time of the file is set to the entry?s last write time.
    /// This method does allows overwriting of an existing file with the same name.
    /// </summary>
    /// 
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only white space,
    /// or contains one or more invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
    /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
    /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
    /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid
    /// (for example, it is on an unmapped drive).</exception>
    /// <exception cref="IOException">destinationFileName exists and overwrite is false.
    /// -or- An I/O error has occurred.
    /// -or- The entry is currently open for writing.
    /// -or- The entry has been deleted from the archive.</exception>
    /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
    /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
    /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
    /// -or- The entry has been compressed using a compression method that is not supported.</exception>
    /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
    /// <param name="destinationFile">The name of the file that will hold the contents of the entry.
    /// The path is permitted to specify relative or absolute path information.
    /// Relative path information is interpreted as relative to the current working directory.</param>
    /// <param name="overwrite">True to indicate overwrite.</param>
    public static void ExtractToFile(this ZipArchiveEntry source, FileEntry destinationFile, Boolean overwrite)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (destinationFile == null)
            throw new ArgumentNullException(nameof(destinationFile));

        // Rely on FileStream's ctor for further checking destinationFileName parameter
        FileMode fMode = overwrite ? FileMode.Create : FileMode.CreateNew;

        using (Stream fs = destinationFile.Open(fMode, FileAccess.Write))
        {
            using (Stream es = source.Open())
            {
                es.CopyTo(fs);
            }
        }
        destinationFile.LastWriteTime = source.LastWriteTime.DateTime;
    }
}