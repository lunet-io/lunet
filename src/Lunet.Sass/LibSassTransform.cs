// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using SharpScss;
using Zio;

namespace Lunet.Sass;

internal class LibSassTransform
{
    public static void Convert(ContentObject file, List<DirectoryEntry> includePaths, SiteObject site)
    {
        var contentType = file.ContentType;

        var content = file.GetOrLoadContent();

        var options = new ScssOptions
        {
            InputFile = (string)file.Path
        };

        var tempIncludePaths = new List<DirectoryEntry>();

        var extensions = new string[] { ".scss", ".sass", ".css" };

        var includedFiles = new List<FileEntry>();
        options.TryImport = (ref string file, string parentpath, out string scss, out string? map) =>
        {
            scss = string.Empty;
            map = string.Empty;

            // From: https://sass-lang.com/documentation/at-rules/import#load-paths
            // Imports will always be resolved relative to the current file first, though.
            // Load paths will only be used if no relative file exists that matches the import.
            // This ensures that you can’t accidentally mess up your relative imports when you add a new library.
            tempIncludePaths.Clear();
            UPath filePath = (UPath)file;
            var directoryName = ((UPath)parentpath).GetDirectory();
            if (!directoryName.IsNull && directoryName.IsAbsolute)
            {
                DirectoryEntry? localDirEntry = null;
                if (site.FileSystem.DirectoryExists(directoryName))
                {
                    localDirEntry = new DirectoryEntry(site.FileSystem, directoryName);
                    if (!tempIncludePaths.Contains(localDirEntry))
                    {
                        tempIncludePaths.Add(localDirEntry);
                    }
                }

                if (site.MetaFileSystem.DirectoryExists(directoryName))
                {
                    localDirEntry = new DirectoryEntry(site.MetaFileSystem, directoryName);
                    if (!tempIncludePaths.Contains(localDirEntry))
                    {
                        tempIncludePaths.Add(localDirEntry);
                    }
                }
            }

            tempIncludePaths.AddRange(includePaths);

            // From libsass, order for ambiguous import:
            // (1) filename as given
            // (2) underscore + given
            // (3) underscore + given + extension
            // (4) given + extension
            // (5) given + _index.scss
            // (6) given + _index.sass
            var ufile = (UPath)file;
            var relativeFolder = ufile.GetDirectory();
            var filename = ufile.GetName();

            bool Resolve(FileEntry entry, out string scss, out string resolvedFile)
            {
                scss = string.Empty;
                resolvedFile = string.Empty;
                if (entry.Exists)
                {
                    scss = entry.ReadAllText();
                    resolvedFile = (string)entry.Path;
                    includedFiles.Add(entry);
                    return true;
                }

                return false;
            }

            foreach (var dirEntry in tempIncludePaths)
            {
                var rootFolder = dirEntry.Path / relativeFolder;

                // (1) filename as given
                if (Resolve(new FileEntry(dirEntry.FileSystem, rootFolder / filename), out scss, out file)) return true;

                // (2) underscore + given
                if (Resolve(new FileEntry(dirEntry.FileSystem, rootFolder / $"_{filename}"), out scss, out file)) return true;

                // (3) underscore + given + extension
                foreach (var extension in extensions)
                    if (Resolve(new FileEntry(dirEntry.FileSystem, rootFolder / $"_{filename}{extension}"), out scss, out file)) return true;

                // (4) given + extension
                foreach (var extension in extensions)
                    if (Resolve(new FileEntry(dirEntry.FileSystem, rootFolder / $"{filename}{extension}"), out scss, out file)) return true;

                // (5) given + _index.scss
                if (Resolve(new FileEntry(dirEntry.FileSystem, rootFolder / $"{filename}" / "_index.scss"), out scss, out file)) return true;

                // (6) given + _index.sass
                if (Resolve(new FileEntry(dirEntry.FileSystem, rootFolder / $"{filename}" / "_index.sass"), out scss, out file)) return true;
            }

            return false;
        };

        var result = SharpScss.Scss.ConvertToCss(content, options);

        file.Content = result.Css;
        file.ChangeContentType(ContentType.Css);

        foreach (var includeFile in includedFiles)
        {
            file.Dependencies.Add(new FileContentDependency(includeFile));
        }
    }
}
