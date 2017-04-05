// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Lunet.Helpers;
using Microsoft.Extensions.Logging;

namespace Lunet.Core
{

    public class FileSystemEventBatchArgs : EventArgs
    {
        public FileSystemEventBatchArgs()
        {
            FileEvents = new List<FileSystemEventArgs>();
        }

        public List<FileSystemEventArgs> FileEvents { get; }
    }

    public class WatcherPlugin : SitePlugin
    {
        private DirectoryInfo baseDirectory;
        private FileSystemWatcher rootDirectoryWatcher;
        private FileSystemWatcher privateMetaWatcher;
        private string privateBaseDirectory;

        private const int SleepForward = 48;
        private const int MillisTimeout = 200;

        private readonly ILogger log;
        private readonly Dictionary<string, FileSystemWatcher> watchers;
        private bool isDisposing;
        private readonly Thread processEventsThread;
        private readonly object batchLock;
        private readonly Stopwatch clock;
        private FileSystemEventBatchArgs batchEvents;
        private readonly ManualResetEvent onClosingEvent;

        public WatcherPlugin(SiteObject site) : base(site)
        {
            watchers = new Dictionary<string, FileSystemWatcher>();
            log = site.Log;
            batchLock = new object();
            processEventsThread = new Thread(ProcessEvents) {IsBackground = true};
            clock = new Stopwatch();
            onClosingEvent = new ManualResetEvent(false);
        }

        private void ProcessEvents()
        {
            while (true)
            {
                FileSystemEventBatchArgs batchEventsCopy = null;

                if (onClosingEvent.WaitOne(SleepForward))
                {
                    break;
                }

                lock (batchLock)
                {
                    if (clock.ElapsedMilliseconds <= MillisTimeout)
                    {
                        continue;
                    }

                    if (batchEvents != null && batchEvents.FileEvents.Count > 0)
                    {
                        batchEventsCopy = batchEvents;
                        batchEvents = null;
                    }
                    else
                    {
                        continue;
                    }
                }

                // TODO: squash events here (to avoid having duplicated events)

                // Invoke listeners
                try
                {
                    FileSystemEvents?.Invoke(this, batchEventsCopy);
                }
                catch (Exception ex)
                {
                    log.LogError($"Unexpected error on SiteWatcher callback. Reason: {ex.GetReason()}");
                }
            }

            onClosingEvent.Reset();
        }

        public event EventHandler<FileSystemEventBatchArgs> FileSystemEvents;

        public void Start()
        {
            if (processEventsThread.IsAlive)
            {
                return;
            }
            privateBaseDirectory = Site.PrivateBaseFolder;
            baseDirectory = Site.BaseFolder;
            rootDirectoryWatcher = CreateFileWatch(Site.BaseFolder, false);
            privateMetaWatcher = CreateFileWatch(Site.PrivateMetaFolder, true);

            foreach (var directory in Site.BaseFolder.Info.EnumerateDirectories())
            {
                CreateFileWatch(directory.FullName, true);
            }

            // Starts the thread
            processEventsThread.Start();
        }

        public void Stop()
        {
            if (processEventsThread.IsAlive)
            {
                onClosingEvent.Set();
                processEventsThread.Join();
            }

            lock (watchers)
            {
                isDisposing = true;
                foreach (var watcher in watchers)
                {
                    DisposeWatcher(watcher.Value);
                }
                watchers.Clear();
            }
        }

        public void WatchForRebuild(Action<SiteObject> rebuild)
        {
            if (rebuild == null) throw new ArgumentNullException(nameof(rebuild));
            Start();

            FileSystemEvents += (sender, args) =>
            {
                if (Site.CanTrace())
                {
                    Site.Trace($"Received file events [{args.FileEvents.Count}]");
                }

                try
                {
                    // Regenerate website
                    // NOTE: we are recreating a full new SiteObject here (not incremental)
                    var siteObject = new SiteObject(Site.LoggerFactory, Site.Plugins);

                    // Copy the plugins from the current site
                    //siteObject.Plugins.Factory.AddRange(Plugins.Factory);
                    //siteObject.Plugins.LoadPlugins();

                    rebuild(siteObject);
                }
                catch (Exception ex)
                {
                    Site.Error($"Unexpected error while reloading the site. Reason: {ex.GetReason()}");
                }
            };
        }

        private bool IsPrivateDirectory(string directory)
        {
            return directory.StartsWith(privateBaseDirectory);
        }

        private FileSystemWatcher CreateFileWatch(string directory, bool recursive)
        {
            // We don't track changes in PrivateBaseDirectory
            if (IsPrivateDirectory(directory))
            {
                return null;
            }
            FileSystemWatcher watcher;
            lock (watchers)
            {
                if (isDisposing)
                {
                    return null;
                }

                if (watchers.TryGetValue(directory, out watcher))
                {
                    return watcher;
                }

                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace($"Tracking file system changed for directory [{directory}]");
                }

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
            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace($"Untrack changes from [{watcher.Path}]");
            }

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
            var dir = new DirectoryInfo(e.FullPath);
            if (IsPrivateDirectory(dir.FullName))
            {
                return;
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace($"File event occured: {e.ChangeType} -> {e.FullPath}");
            }

            lock (watchers)
            {
                var isDirectory = dir.Exists;
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        DisposeWatcher(e.FullPath);
                        break;
                    case WatcherChangeTypes.Created:
                        // Create watcher only for top-level directories
                        if (isDirectory && dir.Parent != null && dir.Parent.FullName == baseDirectory.FullName)
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
                                if (dir.Parent != null && dir.Parent.FullName == baseDirectory.FullName)
                                {
                                    CreateFileWatch(renamed.FullPath, true);
                                }
                            }
                        }
                        break;
                }
            }

            lock (batchLock)
            {
                clock.Restart();

                if (batchEvents == null)
                {
                    batchEvents = new FileSystemEventBatchArgs();
                }

                batchEvents.FileEvents.Add(e);
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