// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Helpers;
using Microsoft.Extensions.Logging;

namespace Lunet.Core
{
    public class SiteWatcher : ManagerBase
    {
        private FileSystemWatcher rootDirectoryWatcher;
        private FileSystemWatcher privateMetaWatcher;

        private readonly ILogger log;
        private readonly Dictionary<string, FileSystemWatcher> watchers;

        internal SiteWatcher(SiteObject site) : base(site)
        {
            watchers = new Dictionary<string, FileSystemWatcher>();
            log = site.LoggerFactory.CreateLogger("watcher");

            rootDirectoryWatcher = CreateFileWatch(Site.BaseDirectory, false);
            privateMetaWatcher = CreateFileWatch(Site.Meta.PrivateDirectory, true);
        }


        public override void InitializeAfterConfig()
        {
            // TODO: Filters unwanted
            foreach (var directory in Site.BaseDirectory.Info.EnumerateDirectories())
            {
                if (directory.FullName.Equals(Site.PrivateBaseDirectory))
                {
                    continue;
                }
                CreateFileWatch(directory.FullName, true);
            }
        }

        private FileSystemWatcher CreateFileWatch(string directory, bool recursive)
        {
            FileSystemWatcher watcher;
            lock (watchers)
            {
                if (watchers.TryGetValue(directory, out watcher))
                {
                    return watcher;
                }

                log.LogTrace($"Tracking file system changed from directory [{directory}]");

                watcher = new FileSystemWatcher
                {
                    Path = directory,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Filter = "",
                    InternalBufferSize = 64*1024,
                    IncludeSubdirectories = recursive
                };

                watcher.Changed += OnFileSystemEvent;
                watcher.Created += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnFileSystemEvent;
                watcher.Error += WatcherOnError;

                watcher.EnableRaisingEvents = true;

                watchers.Add(watcher.Path, watcher);
            }

            return watcher;
        }

        private void DisposeWatcher(string fullPath)
        {
            FileSystemWatcher watcher;
            if (watchers.TryGetValue(fullPath, out watcher))
            {
                DisposeWatcher(watcher);
                watchers.Remove(fullPath);
            }
        }

        private void DisposeWatcher(FileSystemWatcher watcher)
        {
            log.LogTrace($"Untrack changes from [{watcher.Path}]");

            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnFileSystemEvent;
            watcher.Created -= OnFileSystemEvent;
            watcher.Deleted -= OnFileSystemEvent;
            watcher.Renamed -= OnFileSystemEvent;
            watcher.Error -= WatcherOnError;
            watcher.Dispose();
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            log.LogTrace($"File event occured: {e.ChangeType} -> {e.FullPath}");

            lock (watchers)
            {

                var dir = new DirectoryInfo(e.FullPath);
                var isDirectory = dir.Exists;

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        DisposeWatcher(e.FullPath);
                        break;
                    case WatcherChangeTypes.Created:
                        // Create watcher only for top-level directories
                        if (isDirectory && dir.Parent != null && dir.Parent.FullName == Site.BaseDirectory)
                        {
                            CreateFileWatch(dir.FullName, true);
                        }
                        break;
                    case WatcherChangeTypes.Renamed:
                        var renamed = (RenamedEventArgs) e;
                        if (isDirectory)
                        {
                            FileSystemWatcher watcher;
                            if (watchers.TryGetValue(renamed.OldFullPath, out watcher))
                            {
                                DisposeWatcher(renamed.OldFullPath);

                                // Create watcher only for top-level directories
                                if (dir.Parent != null && dir.Parent.FullName == Site.BaseDirectory)
                                {
                                    CreateFileWatch(renamed.FullPath, true);
                                }
                            }
                        }
                        break;
                }
            }
        }
        
        private void WatcherOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            // Not sure if we have something to do with the errors, so don't log them for now
            var watcher = sender as FileSystemWatcher;
            // Site.Trace($"Expection occured for directory watcher [{watcher?.Path}]: {errorEventArgs.GetException().GetReason()}");
        }
    }
}