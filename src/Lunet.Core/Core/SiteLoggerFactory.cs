// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using XenoAtom.Logging;
using XenoAtom.Logging.Writers;

namespace Lunet.Core;

public sealed class SiteLoggerFactory : IDisposable
{
    private static readonly object LockObject = new();
    private static int _instanceCount;
    private static bool _initializedByFactory;
    private bool _disposed;

    public SiteLoggerFactory(bool defaultConsole = true)
    {
        lock (LockObject)
        {
            if (!LogManager.IsInitialized)
            {
                var config = new LogManagerConfig();
                config.RootLogger.MinimumLevel = LogLevel.Trace;
                if (defaultConsole)
                {
                    config.RootLogger.Writers.Add(new TerminalLogWriter());
                }

                LogManager.Initialize(config);
                _initializedByFactory = true;
            }

            _instanceCount++;
        }
    }

    public bool HasErrors { get; private set; }
    
    public Func<string, LogLevel, bool>? LogFilter { get; set; }

    public SiteLogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return new SiteLogger(this, categoryName, LogManager.GetLogger(categoryName));
    }

    internal bool IsEnabled(string category, LogLevel level)
    {
        var filter = LogFilter;
        return filter is null || filter(category, level);
    }

    internal void NotifyLogged(LogLevel level)
    {
        if (level is LogLevel.Error or LogLevel.Fatal)
        {
            HasErrors = true;
        }
    }

    public void Dispose()
    {
        lock (LockObject)
        {
            if (_disposed) return;
            _disposed = true;

            _instanceCount--;
            if (_instanceCount == 0 && _initializedByFactory)
            {
                LogManager.Shutdown();
                _initializedByFactory = false;
            }
        }
    }
}

public sealed class SiteLogger
{
    private readonly SiteLoggerFactory _factory;
    private readonly Logger _logger;

    internal SiteLogger(SiteLoggerFactory factory, string category, Logger logger)
    {
        _factory = factory;
        Category = category;
        _logger = logger;
    }

    public string Category { get; }

    public bool IsEnabled(LogLevel level)
    {
        return _factory.IsEnabled(Category, level) && _logger.IsEnabled(level);
    }

    public void Log(LogLevel level, LogEventId eventId, string message)
    {
        if (!IsEnabled(level)) return;

        switch (level)
        {
            case LogLevel.Trace:
                _logger.Trace(eventId, message);
                break;
            case LogLevel.Debug:
                _logger.Debug(eventId, message);
                break;
            case LogLevel.Info:
                _logger.Info(eventId, message);
                break;
            case LogLevel.Warn:
                _logger.Warn(eventId, message);
                break;
            case LogLevel.Error:
                _logger.Error(eventId, message);
                break;
            case LogLevel.Fatal:
                _logger.Fatal(eventId, message);
                break;
        }

        _factory.NotifyLogged(level);
    }
}
