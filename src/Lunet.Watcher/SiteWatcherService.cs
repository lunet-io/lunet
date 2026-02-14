// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Helpers;
using XenoAtom.CommandLine;
using Zio;

namespace Lunet.Watcher;

public class FileSystemEventBatchArgs : EventArgs
{
    public FileSystemEventBatchArgs()
    {
        FileEvents = new List<FileChangedEventArgs>();
    }

    public List<FileChangedEventArgs> FileEvents { get; }
}

public class WatcherModule : SiteModule
{
    protected override void Configure(SiteApplication application)
    {
        // The run command
        BuildAndWatchCommand = application.AddCommand("build", "Builds the website", newApp =>
        {
            var watchOption = false;
            var singleThreadedOption = false;
            var devOption = false;
            newApp.Add(new HelpOption("h|help"));
            newApp.Add("watch", "Enables watching files and triggering of a new run", _ => watchOption = true);
            newApp.Add("no-threads", "Disables multi-threading", _ => singleThreadedOption = true);
            newApp.Add("dev", "Enables development environment. Default environment is prod.", _ => devOption = true);

            newApp.Add((_, _) =>
            {
                var buildAndWatch = application.CreateCommandRunner<BuildCommandRunner>();
                buildAndWatch.Watch = watchOption;
                buildAndWatch.SingleThreaded = singleThreadedOption;
                buildAndWatch.Development = devOption;
                return ValueTask.FromResult(0);
            });

        });
    }

    public Command BuildAndWatchCommand { get; private set; } = null!;
}

public class SiteWatcherService : ISiteService
{
    private DirectoryEntry _siteDirectory = null!;

    private const int SleepForward = 48;
    private const int MillisTimeout = 200;

    private readonly Dictionary<DirectoryEntry, IFileSystemWatcher> _watchers;
    private bool _isDisposing;
    private readonly Thread _processEventsThread;
    private readonly object _batchLock;
    private readonly Stopwatch _clock;
    private FileSystemEventBatchArgs? _batchEvents;
    private readonly ManualResetEvent _onClosingEvent;
    private bool _threadStarted = false;
    private readonly SiteConfiguration _siteConfig;

    public SiteWatcherService(SiteConfiguration siteConfig)
    {
        _siteConfig = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
        _watchers = new Dictionary<DirectoryEntry, IFileSystemWatcher>();
        _batchLock = new object();
        _processEventsThread = new Thread(ProcessEvents) { IsBackground = true };
        _clock = new Stopwatch();
        _onClosingEvent = new ManualResetEvent(false);
        FileSystemEvents = new BlockingCollection<FileSystemEventBatchArgs>();
    }

    public BlockingCollection<FileSystemEventBatchArgs> FileSystemEvents { get; }

    public Func<UPath, bool>? IsHandlingPath;
        
    public static async Task<RunnerResult> RunAsync(SiteRunner runner, CancellationToken cancellationToken)
    {
        var site = runner.CurrentSite;
        if (site is null)
        {
            return RunnerResult.ExitWithError;
        }
        var runnerResult = RunnerResult.Continue;
        var watcherService = runner.GetService<SiteWatcherService>();

        if (watcherService == null)
        {
            watcherService = new SiteWatcherService(runner.Config)
            {
                IsHandlingPath = site.IsHandlingPath
            };
            watcherService.Start();
            runner.RegisterService(watcherService);
            runner.Config.Info("File watcher started and waiting for file changes.");
        }
        else
        {
            watcherService.IsHandlingPath = site.IsHandlingPath;
        }

        try
        {
            var events = watcherService.FileSystemEvents.Take(cancellationToken);

            if (runner.Config.CanTrace())
            {
                runner.Config.Trace($"Received file events [{events.FileEvents.Count}].");
            }

            runnerResult = RunnerResult.Continue;
        }
        catch (OperationCanceledException)
        {
            watcherService.Dispose();
            if (cancellationToken.IsCancellationRequested)
            {
                runnerResult = RunnerResult.Exit;
            }
        }

        return runnerResult;
    }
        
    public void Start()
    {
        if (_processEventsThread.IsAlive)
        {
            return;
        }

        _siteDirectory = new DirectoryEntry(_siteConfig.FileSystems.FileSystem, UPath.Root);

        WatchFolder(_siteDirectory);

        // Starts the thread
        _processEventsThread.Start();
    }
        
    private void ProcessEvents()
    {
        _threadStarted = true;
        while (true)
        {
            FileSystemEventBatchArgs? batchEventsCopy = null;

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
                if (batchEventsCopy is null)
                {
                    continue;
                }

                SquashAndLogChanges(batchEventsCopy);

                // Squash can discard events (e.g if files excluded)
                if (batchEventsCopy.FileEvents.Count > 0)
                {
                    FileSystemEvents.Add(batchEventsCopy);
                }
            }
            catch (Exception ex)
            {
                _siteConfig.Error(ex, $"Unexpected error on SiteWatcher callback. Reason: {ex.GetReason()}");
            }
        }

        _onClosingEvent.Reset();
    }

    private void SquashAndLogChanges(FileSystemEventBatchArgs args)
    {
        var list = new List<SimpleFileChangedEventArgs>();
        foreach (var arg in args.FileEvents)
        {
            var simpleArg = new SimpleFileChangedEventArgs(arg);
            var index = list.IndexOf(simpleArg);
            if (index >= 0)
            {
                list.RemoveAt(index);
            }
            list.Add(simpleArg);
        }

        args.FileEvents.Clear();

        foreach (var change in list)
        {
            var e = change.Args;
            if (!IsPathToWatch(e.FullPath))
            {
                continue;
            }

            args.FileEvents.Add(e);
            if (_siteConfig.CanInfo())
            {
                _siteConfig.Info($"File event occured: {e.ChangeType} -> {e.FullPath}");
            }
        }
    }

    private struct SimpleFileChangedEventArgs : IEquatable<SimpleFileChangedEventArgs>
    {
        public SimpleFileChangedEventArgs(FileChangedEventArgs args) : this()
        {
            FileSystem = args.FileSystem;
            FullPath = args.FullPath;
            Args = args;
        }

        /// <summary>
        /// The filesystem originating this change.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Absolute path to the file or directory.
        /// </summary>
        public UPath FullPath { get; }

        public FileChangedEventArgs Args { get; }

        public bool Equals(SimpleFileChangedEventArgs other)
        {
            return Equals(FileSystem, other.FileSystem) && FullPath.Equals(other.FullPath);
        }

        public override bool Equals(object? obj)
        {
            return obj is SimpleFileChangedEventArgs other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileSystem, FullPath);
        }

        public static bool operator ==(SimpleFileChangedEventArgs left, SimpleFileChangedEventArgs right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SimpleFileChangedEventArgs left, SimpleFileChangedEventArgs right)
        {
            return !left.Equals(right);
        }
    }

    private bool IsPathToWatch(UPath path)
    {
        return path == _siteConfig.FileSystems.ConfigFile.Path || (IsHandlingPath?.Invoke(path) ?? false);
    }

    private void WatchFolder(DirectoryEntry entry)
    {
        bool isEntryLunet = entry.Name == SiteFileSystems.LunetFolderName;

        if (IsPathToWatch(entry.Path))
        {
            CreateFileWatch(entry);
        }

        foreach (var directory in entry.EnumerateDirectories())
        {
            if (isEntryLunet)
            {
                if (directory.Path.IsInDirectory(SiteFileSystems.BuildFolder, true) || directory.Name.StartsWith("new"))
                {
                    if (_siteConfig.CanTrace())
                    {
                        _siteConfig.Trace($"Skipping {directory.FullName}");
                    }
                    continue;
                }
            }

            WatchFolder(directory);
        }
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

        _siteConfig.Info("File watcher stopped.");
    }

    private bool IsOutputDirectory(UPath path)
    {
        return path.IsInDirectory(SiteFileSystems.BuildFolder, true);
    }

    private void CreateFileWatch(DirectoryEntry directory)
    {
        lock (_watchers)
        {
            if (_isDisposing)
            {
                return;
            }

            if (_watchers.TryGetValue(directory, out var watcher))
            {
                return;
            }

            if (_siteConfig.CanTrace())
            {
                _siteConfig.Trace($"Tracking file system changed for directory [{directory}]");
            }

            watcher = directory.FileSystem.Watch(directory.Path);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "";
            watcher.InternalBufferSize = 64 * 1024;
            watcher.IncludeSubdirectories = false;
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
        if (_siteConfig.CanTrace())
        {
            _siteConfig.Trace($"Untrack changes from [{watcher.Path}]");
        }

        watcher.EnableRaisingEvents = false;
        watcher.Changed -= OnFileSystemEvent;
        watcher.Created -= OnFileSystemEvent;
        watcher.Deleted -= OnFileSystemEvent;
        watcher.Renamed -= OnFileSystemEvent;
        watcher.Error -= WatcherOnError;
        watcher.Dispose();
    }

    private void OnFileSystemEvent(object? sender, FileChangedEventArgs e)
    {
        // Don't log events until the thread is started
        if (!_threadStarted) return;

        var dir = new DirectoryEntry(e.FileSystem, e.FullPath);

        if (IsOutputDirectory(e.FullPath))
        {
            return;
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
                    if (isDirectory && dir.Path.GetDirectory() == _siteDirectory.Path)
                    {
                        WatchFolder(dir);
                    }
                    break;
                case WatcherChangeTypes.Renamed:
                    var renamed = (FileRenamedEventArgs)e;
                    if (isDirectory)
                    {
                        var previousDirectory = new DirectoryEntry(renamed.FileSystem, renamed.OldFullPath);
                        if (_watchers.TryGetValue(previousDirectory, out _))
                        {
                            DisposeWatcher(previousDirectory);

                            // Create watcher only for top-level directories
                            if (dir.Path.GetDirectory() == _siteDirectory.Path)
                            {
                                WatchFolder(dir);
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

    private void WatcherOnError(object? sender, FileSystemErrorEventArgs errorEventArgs)
    {
        // Not sure if we have something to do with the errors, so don't log them for now
        var watcher = sender as IFileSystemWatcher;
        // _config.Trace($"Expection occured for directory watcher [{watcher?.Path}]: {errorEventArgs.GetException().GetReason()}");
    }

    public void Dispose()
    {
        Stop();
        _onClosingEvent.Dispose();
    }
}
