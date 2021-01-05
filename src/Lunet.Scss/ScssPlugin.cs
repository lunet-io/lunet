// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Lunet.Layouts;
using SharpScss;
using Zio;

namespace Lunet.Scss
{
    public class ScssModule : SiteModule<ScssPlugin>
    {
    }

    public class ScssPlugin : SitePlugin, ILayoutConverter
    {
        public static readonly ContentType ScssType = new ContentType("scss");

        public ScssPlugin(SiteObject site, LayoutPlugin layoutPlugin) : base(site)
        {
            Includes = new PathCollection();
            SetValue("includes", Includes, true);
            site.SetValue("scss", this, true);
            layoutPlugin.Processor.RegisterConverter(ScssType, this);
        }

        public override string Name => "scss";

        public PathCollection Includes { get; }

        public bool ShouldConvertIfNoLayout => true;

        public void Convert(ContentObject file)
        {
            var contentType = file.ContentType;

            // This plugin is only working on scss files
            if (contentType != ScssType)
            {
                return;
            }

            file.Content ??= file.SourceFile.ReadAllText();

            var content = file.Content;

            var options = new ScssOptions();
            options.InputFile = (string)file.Path;

            var includePaths = new List<DirectoryEntry>();

            foreach (var pathObj in Includes)
            {
                var path = pathObj as string;
                if (path != null && UPath.TryParse(path, out var validPath) && Site.MetaFileSystem.DirectoryExists(validPath))
                {
                    includePaths.Add(new DirectoryEntry(Site.MetaFileSystem, validPath));
                }
                else
                {
                    Site.Error($"Invalid folder path `{pathObj}` found in site.scss.includes.");
                }
            }

            var tempIncludePaths = new List<DirectoryEntry>();

            var extensions = new string[] { ".scss", ".sass", ".css" };

            var includedFiles = new List<FileEntry>();
            options.TryImport = (ref string file, string parentpath, out string scss, out string map) =>
            {
                scss = null;
                map = null;

                // From: https://sass-lang.com/documentation/at-rules/import#load-paths
                // Imports will always be resolved relative to the current file first, though.
                // Load paths will only be used if no relative file exists that matches the import.
                // This ensures that you can’t accidentally mess up your relative imports when you add a new library.
                tempIncludePaths.Clear();
                UPath filePath = (UPath)file;
                var directoryName = ((UPath)parentpath).GetDirectory();
                if (!directoryName.IsNull && directoryName.IsAbsolute)
                {
                    DirectoryEntry localDirEntry = null;
                    if (Site.FileSystem.DirectoryExists(directoryName))
                    {
                        localDirEntry = new DirectoryEntry(Site.FileSystem, directoryName);
                        if (!tempIncludePaths.Contains(localDirEntry))
                        {
                            tempIncludePaths.Add(localDirEntry);
                        }
                    }

                    if (Site.MetaFileSystem.DirectoryExists(directoryName))
                    {
                        localDirEntry = new DirectoryEntry(Site.MetaFileSystem, directoryName);
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

                bool Resolve(FileEntry entry, out string scss, out string file)
                {
                    scss = null;
                    file = null;
                    if (entry.Exists)
                    {
                        scss = entry.ReadAllText();
                        file = (string)entry.Path;
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
}