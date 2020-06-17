﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lunet.Core;
using Lunet.Helpers;
using Microsoft.Extensions.Logging;
using Zio;

namespace Lunet.Watcher
{
    public class FileSystemEventBatchArgs : EventArgs
    {
        public FileSystemEventBatchArgs()
        {
            FileEvents = new List<FileChangedEventArgs>();
        }

        public List<FileChangedEventArgs> FileEvents { get; }
    }

    public class WatcherPlugin : SitePlugin
    {
        private DirectoryEntry _siteDirectory;

        private const int SleepForward = 48;
        private const int MillisTimeout = 200;

        private readonly ILogger _log;
        private readonly Dictionary<DirectoryEntry, IFileSystemWatcher> _watchers;
        private bool _isDisposing;
        private readonly Thread _processEventsThread;
        private readonly object _batchLock;
        private readonly Stopwatch _clock;
        private FileSystemEventBatchArgs _batchEvents;
        private readonly ManualResetEvent _onClosingEvent;

        public WatcherPlugin(SiteObject site) : base(site)
        {
            _watchers = new Dictionary<DirectoryEntry, IFileSystemWatcher>();
            _log = site.Log;
            _batchLock = new object();
            _processEventsThread = new Thread(ProcessEvents) {IsBackground = true};
            _clock = new Stopwatch();
            _onClosingEvent = new ManualResetEvent(false);
        }

        private void ProcessEvents()
        {
            while (true)
            {
                FileSystemEventBatchArgs batchEventsCopy = null;

                if (_onClosingEvent.WaitOne(SleepForward))
                {
                    break;
                }

                lock (_batchLock)
                {
                    if (_clock.ElapsedMilliseconds <= MillisTimeout)
                    {
                        continue;
                    }

                    if (_batchEvents != null && _batchEvents.FileEvents.Count > 0)
                    {
                        batchEventsCopy = _batchEvents;
                        _batchEvents = null;
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
                    _log.LogError($"Unexpected error on SiteWatcher callback. Reason: {ex.GetReason()}");
                }
            }

            _onClosingEvent.Reset();
        }

        public event EventHandler<FileSystemEventBatchArgs> FileSystemEvents;

        public void Start()
        {
            if (_processEventsThread.IsAlive)
            {
                return;
            }
            _siteDirectory = new DirectoryEntry(Site.FileSystem, UPath.Root);
            CreateFileWatch(_siteDirectory, false);

            foreach (var directory in Site.SiteFileSystem.EnumerateDirectoryEntries(UPath.Root))
            {
                CreateFileWatch(directory, true);
            }

            // Starts the thread
            _processEventsThread.Start();
        }

        public void Stop()
        {
            if (_processEventsThread.IsAlive)
            {
                _onClosingEvent.Set();
                _processEventsThread.Join();
            }

            lock (_watchers)
            {
                _isDisposing = true;
                foreach (var watcher in _watchers)
                {
                    DisposeWatcher(watcher.Value);
                }
                _watchers.Clear();
            }
        }

        private bool IsOutputDirectory(UPath path)
        {
            return path.IsInDirectory(SiteObject.TempFolder, true);
        }

        private void CreateFileWatch(DirectoryEntry directory, bool recursive)
        {
            if (IsOutputDirectory(directory.Path))
            {
                Console.WriteLine($"Discard directory {directory}");
                return;
            }

            lock (_watchers)
            {
                if (_isDisposing)
                {
                    return;
                }

                IFileSystemWatcher watcher;
                if (_watchers.TryGetValue(directory, out watcher))
                {
                    return;
                }

                if (_log.IsEnabled(LogLevel.Trace))
                {
                    _log.LogTrace($"Tracking file system changed for directory [{directory}]");
                }

                watcher = directory.FileSystem.Watch(directory.Path);
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "";
                watcher.InternalBufferSize = 64 * 1024;
                watcher.IncludeSubdirectories = recursive;
                watcher.Changed += OnFileSystemEvent;
                watcher.Created += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnFileSystemEvent;
                watcher.Error += WatcherOnError;

                watcher.EnableRaisingEvents = true;

                _watchers.Add(directory, watcher);
            }
        }

        private void DisposeWatcher(DirectoryEntry entry)
        {
            lock (_watchers)
            {
                if (_watchers.TryGetValue(entry, out var watcher))
                {
                    DisposeWatcher(watcher);
                    _watchers.Remove(entry);
                }
            }
        }

        private void DisposeWatcher(IFileSystemWatcher watcher)
        {
            if (_log.IsEnabled(LogLevel.Trace))
            {
                _log.LogTrace($"Untrack changes from [{watcher.Path}]");
            }

            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnFileSystemEvent;
            watcher.Created -= OnFileSystemEvent;
            watcher.Deleted -= OnFileSystemEvent;
            watcher.Renamed -= OnFileSystemEvent;
            watcher.Error -= WatcherOnError;
            watcher.Dispose();
        }

        private void OnFileSystemEvent(object sender, FileChangedEventArgs e)
        {
            var dir = new DirectoryEntry(e.FileSystem, e.FullPath);

            if (IsOutputDirectory(e.FullPath))
            {
                return;
            }

            //if (_log.IsEnabled(LogLevel.Trace))
            {
                Console.WriteLine($"File event occured: {e.ChangeType} -> {e.FullPath}");
                _log.LogInformation($"File event occured: {e.ChangeType} -> {e.FullPath}");
            }

            lock (_watchers)
            {
                var isDirectory = dir.Exists;
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        if (isDirectory)
                        {
                            DisposeWatcher(dir);
                        }
                        break;
                    case WatcherChangeTypes.Created:
                        // Create watcher only for top-level directories
                        if (isDirectory && dir.Parent != null && dir.Parent == _siteDirectory)
                        {
                            CreateFileWatch(dir, true);
                        }
                        break;
                    case WatcherChangeTypes.Renamed:
                        var renamed = (FileRenamedEventArgs) e;
                        if (isDirectory)
                        {
                            var previousDirectory = new DirectoryEntry(renamed.FileSystem, renamed.OldFullPath);
                            if (_watchers.TryGetValue(previousDirectory, out _))
                            {
                                DisposeWatcher(previousDirectory);

                                // Create watcher only for top-level directories
                                if (dir.Parent != null && dir.Parent == _siteDirectory)
                                {
                                    CreateFileWatch(dir, true);
                                }
                            }
                        }
                        break;
                }
            }

            lock (_batchLock)
            {
                _clock.Restart();

                if (_batchEvents == null)
                {
                    _batchEvents = new FileSystemEventBatchArgs();
                }

                _batchEvents.FileEvents.Add(e);
            }
        }
        
        private void WatcherOnError(object sender, FileSystemErrorEventArgs errorEventArgs)
        {
            // Not sure if we have something to do with the errors, so don't log them for now
            var watcher = sender as IFileSystemWatcher;
            // Site.Trace($"Expection occured for directory watcher [{watcher?.Path}]: {errorEventArgs.GetException().GetReason()}");
        }
    }
}