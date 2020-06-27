using System.Collections.Generic;
using Lunet.Core;
using SharpScss;
using Zio;

namespace Lunet.Scss
{
    public class ScssProcessor : ContentProcessor<ScssPlugin>
    {
        public static readonly ContentType ScssType = new ContentType("scss");

        public ScssProcessor(ScssPlugin plugin) : base(plugin)
        {
        }

        public override ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage)
        {
            var contentType = file.ContentType;

            // This plugin is only working on scss files
            if (contentType != ScssType)
            {
                return ContentResult.None;
            }

            if (file.Content == null)
            {
                file.Content = file.SourceFile.ReadAllText();
            }

            var content = file.Content;

            var options = new ScssOptions();
            options.InputFile = (string)file.Path;

            var includePaths = new List<DirectoryEntry>();

            foreach (var pathObj in Plugin.Includes)
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

            var extensions = new string[] {"", ".scss", ".sass", ".css"};


            var includedFiles = new List<FileEntry>();

            options.TryImport = (string file, string parentpath, out string scss, out string map) =>
            {
                scss = null;
                map = null;

                // From: https://sass-lang.com/documentation/at-rules/import#load-paths
                // Imports will always be resolved relative to the current file first, though.
                // Load paths will only be used if no relative file exists that matches the import.
                // This ensures that you can’t accidentally mess up your relative imports when you add a new library.
                tempIncludePaths.Clear();
                UPath filePath = (UPath)file;
                var directoryName = ((UPath) parentpath).GetDirectory();
                if (!directoryName.IsNull && directoryName.IsAbsolute)
                {
                    DirectoryEntry localDirEntry = null;
                    if (Site.FileSystem.DirectoryExists(directoryName))
                    {
                        localDirEntry = new DirectoryEntry(Site.FileSystem, directoryName);
                    }
                    else if (Site.MetaFileSystem.DirectoryExists(directoryName))
                    {
                        localDirEntry = new DirectoryEntry(Site.MetaFileSystem, directoryName);
                    }

                    if (localDirEntry != null && tempIncludePaths.Contains(localDirEntry))
                    {
                        tempIncludePaths.Add(localDirEntry);
                    }
                }

                tempIncludePaths.AddRange(includePaths);

                foreach (var dirEntry in tempIncludePaths)
                {
                    foreach (var extension in extensions)
                    {
                        var localFile = file + extension;
                        var newFilePath = dirEntry.Path / localFile;
                        if (dirEntry.FileSystem.FileExists(newFilePath))
                        {
                            scss = dirEntry.FileSystem.ReadAllText(newFilePath);
                            includedFiles.Add(new FileEntry(dirEntry.FileSystem, newFilePath));
                            return true;
                        }

                        // Try for partials _
                        var localFileUPath = (UPath) localFile;
                        if (!localFileUPath.GetDirectory().IsNull)
                        {
                            localFileUPath = localFileUPath.GetDirectory() / ("_" + localFileUPath.GetName());
                        }
                        else
                        {
                            localFileUPath = "_" + localFile;
                        }
                        newFilePath = dirEntry.Path / localFileUPath;
                        if (dirEntry.FileSystem.FileExists(newFilePath))
                        {
                            scss = dirEntry.FileSystem.ReadAllText(newFilePath);
                            includedFiles.Add(new FileEntry(dirEntry.FileSystem, newFilePath));
                            return true;
                        }
                    }
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

            return ContentResult.Continue;
        }
    }
}