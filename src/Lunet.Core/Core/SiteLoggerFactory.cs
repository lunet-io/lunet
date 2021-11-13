// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;

namespace Lunet.Core;

public class SiteLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _defaultFactory;
    private readonly ServiceProvider _serviceProvider;

    public SiteLoggerFactory(Action<ILoggingBuilder> logBuilderAction = null, bool defaultConsole = true)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConfiguration();
            builder.AddProvider(new LoggerProviderIntercept(this))
                .AddFilter(LogFilterImpl);

            logBuilderAction?.Invoke(builder);

            if (defaultConsole)
            {
                // Similar to builder.AddSimpleConsole
                // But we are using our own console that stays on the same line if the message doesn't have new lines
                builder.AddConsoleFormatter<SimpleConsoleFormatter, SimpleConsoleFormatterOptions>(configure => { configure.SingleLine = true; });
                builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());
                LoggerProviderOptions.RegisterProviderOptions<ConsoleLoggerOptions, ConsoleLoggerProvider>(builder.Services);
            }
        });
        _serviceProvider = services.BuildServiceProvider();
        _defaultFactory = _serviceProvider.GetService<ILoggerFactory>();
    }

    public bool HasErrors { get; private set; }
        
    public Func<string, LogLevel, bool> LogFilter { get; set; }

    private bool LogFilterImpl(string category, LogLevel level)
    {
        var logFilter = LogFilter;
        if (logFilter != null)
        {
            return logFilter(category, level);
        }

        return true;
    }

    private class LoggerProviderIntercept : ILoggerProvider
    {
        private readonly SiteLoggerFactory _factory;

        public LoggerProviderIntercept(SiteLoggerFactory factory)
        {
            this._factory = factory;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new LoggerIntercept(_factory);
        }
    }

    private class LoggerIntercept : ILogger
    {
        private readonly SiteLoggerFactory _factory;

        public LoggerIntercept(SiteLoggerFactory factory)
        {
            _factory = factory;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
            {
                _factory.HasErrors = true;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Critical || logLevel == LogLevel.Error;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _defaultFactory.Dispose();
        _serviceProvider.Dispose();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _defaultFactory.CreateLogger(categoryName);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        _defaultFactory.AddProvider(provider);
    }
}