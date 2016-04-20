// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lunet.Helpers;
using Lunet.Scripts;

namespace Lunet.Core
{
    public class SiteGenerator
    {
        private readonly HashSet<string> previousOutputDirectories;
        private readonly HashSet<string> previousOutputFiles;
        private readonly Dictionary<string, FileInfo> filesWritten;
        private bool isInitialized;
        private readonly Stopwatch clock;
        private readonly Stopwatch totalDuration;

        internal SiteGenerator(SiteObject site)
        {
            this.Site = site;
            previousOutputDirectories = new HashSet<string>();
            previousOutputFiles = new HashSet<string>();
            Scripts = Site.Scripts;
            filesWritten = new Dictionary<string, FileInfo>();
            clock = new Stopwatch();
            totalDuration = new Stopwatch();
        }

        private ScriptManager Scripts { get; }

        public SiteObject Site { get; }

        public bool TryCopyFile(FileInfo fromFile, string outputPath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            fromFile = fromFile.Normalize();
            var outputFile = new FileInfo(Path.Combine(Site.OutputDirectory, outputPath)).Normalize();
            TrackDestination(outputFile, fromFile);

            if (!outputFile.Exists || (fromFile.LastWriteTime > outputFile.LastWriteTime))
            {
                CreateDirectory(outputFile.Directory);

                if (Site.CanTrace())
                {
                    Site.Trace($"Copy file from [{Site.GetRelativePath(fromFile.FullName, PathFlags.File|PathFlags.Normalize)} to [{outputPath}]");
                }

                fromFile.CopyTo(outputFile.FullName, true);
                return true;
                // Update statistics
                //stat.Static = true;
                //stat.OutputBytes += fromFile.Length;
            }
            return false;
        }

        public bool TryCopyContentToOutput(ContentObject fromFile, string relativePath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            relativePath = PathUtil.NormalizeRelativePath(relativePath, false);
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException($"Path [{relativePath}] cannot be rooted", nameof(relativePath));

            var outputFile = new FileInfo(Path.Combine(Site.OutputDirectory, relativePath)).Normalize();
            var outputDir = outputFile.Directory;
            if (outputDir == null)
            {
                throw new ArgumentException("Output directory cannot be empty", nameof(relativePath));
            }
            TrackDestination(outputFile, fromFile.SourceFileInfo);

            var stat = Site.Statistics.GetContentStat(fromFile);

            CreateDirectory(outputDir);

            clock.Restart();
            try
            {
                // If the source file is not newer than the destination file, don't overwrite it
                if (fromFile.Content != null)
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{Site.GetRelativePath(outputFile.FullName, PathFlags.File)}]");
                    }

                    using (var writer = new StreamWriter(outputFile.FullName))
                    {
                        writer.Write(fromFile.Content);
                        writer.Flush();

                        // Update statistics
                        stat.Static = false;
                        stat.OutputBytes += writer.BaseStream.Length;
                    }
                }
                else if (!outputFile.Exists || (fromFile.ModifiedTime > outputFile.LastWriteTime))
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{Site.GetRelativePath(outputFile.FullName, PathFlags.File)}]");
                    }

                    fromFile.SourceFileInfo.CopyTo(outputFile.FullName, true);

                    // Update statistics
                    stat.Static = true;
                    stat.OutputBytes += fromFile.Length;
                }
            }
            catch (Exception ex)
            {
                Site.Error($"Unable to copy file [{Site.GetRelativePath(fromFile.SourceFileInfo.FullName, PathFlags.File)}] to [{outputFile}]. Reason:{ex.GetReason()}");
                return false;
            }
            finally
            {
                stat.OutputTime += clock.Elapsed;
            }

            return true;
        }

        public void TrackDestination(FileInfo outputFile, FileInfo sourceFile)
        {
            if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));
            if (sourceFile == null) throw new ArgumentNullException(nameof(sourceFile));

            var relativePath = Site.GetRelativePath(outputFile.FullName, PathFlags.File | PathFlags.Normalize);
            var outputFilename = outputFile.FullName;


            FileInfo previousSourceFile;
            if (filesWritten.TryGetValue(outputFilename, out previousSourceFile))
            {
                Site.Error($"The content [{Site.GetRelativePath(previousSourceFile.FullName, PathFlags.File|PathFlags.Normalize)}] and [{Site.GetRelativePath(sourceFile.FullName, PathFlags.File | PathFlags.Normalize)}] have the same Url output [{relativePath}]");
            }
            else
            {
                filesWritten.Add(outputFilename, sourceFile);
            }

            // If the directory is used for a new file, remove it from the list of previous directories
            // Note that we remove even if things are not working after, so that previous files are kept 
            // in case of an error
            var previousDir = outputFile.Directory;
            while (previousDir != null)
            {
                previousOutputDirectories.Remove(previousDir.FullName);
                previousDir = previousDir.Parent;
            }
            previousOutputFiles.Remove(outputFilename);
        }

        public void Initialize()
        {
            totalDuration.Restart();

            // We collect all previous file entries in the output directory
            CollectPreviousFileEntries();

            filesWritten.Clear();
            Site.Statistics.Reset();

            if (isInitialized)
            {
                return;
            }
            isInitialized = true;

            foreach (var manager in Site.Managers)
            {
                manager.InitializeBeforeConfig();
            }

            // If we have any errors, early exit
            if (Site.HasErrors)
            {
                return;
            }

            // We then actually load the config
            Scripts.TryImportScriptFromFile(Site.ConfigFile, Site.DynamicObject, true);

            // If we have any errors, early exit
            if (Site.HasErrors)
            {
                return;
            }

            foreach (var manager in Site.Managers)
            {
                manager.InitializeAfterConfig();
            }
        }

        public void Run()
        {
            Initialize();

            // If we have any errors, early exit
            if (Site.HasErrors)
            {
                return;
            }

            // List all files
            Site.Load();

            // Start to process files
            Site.Plugins.BeginProcess();
            try
            {
                // Process static files
                Site.Plugins.ProcessPages(Site.StaticFiles);

                // Process pages (files with front matter)
                Site.Plugins.ProcessPages(Site.Pages);
            }
            finally
            {
                // End the process
                Site.Plugins.EndProcess();
            }

            // Remove output files
            CleanupOutputFiles();

            // Update statistics
            totalDuration.Stop();
            Site.Statistics.TotalTime = totalDuration.Elapsed;
        }

        public void CreateDirectory(DirectoryInfo directory)
        {
            if (!directory.Exists)
            {
                if (Site.CanTrace())
                {
                    Site.Trace($"Create directory [{Site.GetRelativePath(directory.FullName, PathFlags.Directory)}]");
                }

                try
                {
                    directory.Create();
                }
                catch (Exception ex)
                {
                    Site.Error($"Unable to create directory [{Site.GetRelativePath(directory.FullName, PathFlags.Directory)}]. Reason:{ex.GetReason()}");
                }
            }
        }

        private void CleanupOutputFiles()
        {
            // Remove all previous files that have not been generated
            foreach (var outputFilename in previousOutputFiles)
            {
                var outputFile = new FileInfo(outputFilename);
                try
                {
                    if (outputFile.Exists)
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete file [{Site.GetRelativePath(outputFile.FullName, PathFlags.File)}]");
                        }
                        outputFile.Delete();
                    }
                }
                catch (Exception)
                {
                }
            }

            foreach (var outputDirectory in previousOutputDirectories)
            {
                try
                {
                    if (Directory.Exists(outputDirectory))
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete directory [{Site.GetRelativePath(outputDirectory, PathFlags.Directory)}]");
                        }
                        Directory.Delete(outputDirectory);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void CollectPreviousFileEntries()
        {
            var outputDirectoryInfo = new DirectoryInfo(Site.OutputDirectory);
            previousOutputDirectories.Clear();
            previousOutputFiles.Clear();

            if (!outputDirectoryInfo.Exists)
            {
                return;
            }

            var directories = new Queue<DirectoryInfo>();
            directories.Enqueue(outputDirectoryInfo);

            while (directories.Count > 0)
            {
                var nextDirectory = directories.Dequeue();

                if (nextDirectory != outputDirectoryInfo)
                {
                    previousOutputDirectories.Add(nextDirectory.FullName);
                }

                foreach (var entry in nextDirectory.EnumerateFileSystemInfos())
                {
                    if (entry is FileInfo)
                    {
                        previousOutputFiles.Add(((FileInfo)entry).FullName);
                    }
                    else if (entry is DirectoryInfo)
                    {
                        directories.Enqueue((DirectoryInfo)entry);
                    }
                }
            }
        }
    }
}