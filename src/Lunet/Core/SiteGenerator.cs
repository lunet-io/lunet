﻿// Copyright (c) Alexandre Mutel. All rights reserved.
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
        private readonly Dictionary<string, ContentObject> filesWritten;
        private bool isInitialized;
        private readonly Stopwatch clock;
        private readonly Stopwatch totalDuration;

        internal SiteGenerator(SiteObject site)
        {
            this.Site = site;
            previousOutputDirectories = new HashSet<string>();
            previousOutputFiles = new HashSet<string>();
            Scripts = Site.Scripts;
            filesWritten = new Dictionary<string, ContentObject>();
            clock = new Stopwatch();
            totalDuration = new Stopwatch();
        }

        private ScriptManager Scripts { get; }

        public SiteObject Site { get; }

        public bool TryCopyContentToOutput(ContentObject fromFile, string relativePath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            var outputFile = new FileInfo(Path.Combine(Site.OutputDirectory, relativePath)).FullName;
            var outputDir = new DirectoryInfo(Path.GetDirectoryName(outputFile));

            ContentObject previousContent;
            var stat = Site.Statistics.GetContentStat(fromFile);

            if (filesWritten.TryGetValue(relativePath, out previousContent))
            {
                Site.Error($"The content [{previousContent.Path}] and [{fromFile.Path}] have the same Url output [{relativePath}]");
            }
            else
            {
                filesWritten.Add(relativePath, fromFile);
            }

            // If the directory is used for a new file, remove it from the list of previous directories
            // Note that we remove even if things are not working after, so that previous files are kept 
            // in case of an error
                var previousDir = outputDir;
            while (previousDir != null)
            {
                previousOutputDirectories.Remove(previousDir.FullName);
                previousDir = previousDir.Parent;
            }
            previousOutputFiles.Remove(outputFile);

            bool directoryAlreadyExist = true;
            try
            {
                if (!outputDir.Exists)
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Create directory [{Site.GetRelativePath(outputDir.FullName, PathFlags.Directory)}]");
                    }
                    outputDir.Create();
                    directoryAlreadyExist = false;
                }
            }
            catch (Exception ex) // wide catch
            {
                Site.Error($"Unable to create directory [{Site.GetRelativePath(outputDir.FullName, PathFlags.Directory)}]. Reason:{ex.GetReason()}");
                return false;
            }

            clock.Restart();
            try
            {
                // If the source file is not newer than the destination file, don't overwrite it
                if (fromFile.Content != null)
                {

                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{Site.GetRelativePath(outputFile, PathFlags.File)}]");
                    }

                    using (var writer = new StreamWriter(outputFile))
                    {
                        writer.Write(fromFile.Content);
                        writer.Flush();

                        // Update statistics
                        stat.Static = false;
                        stat.OutputBytes += writer.BaseStream.Length;
                    }
                }
                else if (!directoryAlreadyExist || !File.Exists(outputFile) ||
                         (fromFile.ModifiedTime > File.GetLastWriteTime(outputFile)))
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{Site.GetRelativePath(outputFile, PathFlags.File)}]");
                    }

                    fromFile.SourceFileInfo.CopyTo(outputFile, true);

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

        public void Initialize()
        {
            totalDuration.Restart();
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

            // We collect all previous file entries in the output directory
            CollectPreviousFileEntries();

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
                try
                {
                    directory.Create();
                }
                catch (Exception ex)
                {
                    Site.Error($"Unable to create [{directory}] directory. Reason:{ex.GetReason()}");
                }
            }
        }

        private void CleanupOutputFiles()
        {
            // Remove all previous files that have not been generated
            foreach (var outputFile in previousOutputFiles)
            {
                try
                {
                    if (File.Exists(outputFile))
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete file [{Site.GetRelativePath(outputFile, PathFlags.File)}]");
                        }
                        File.Delete(outputFile);
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
                        previousOutputFiles.Add(entry.FullName);
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