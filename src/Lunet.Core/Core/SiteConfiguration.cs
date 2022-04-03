// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Lunet.Core;

/// <summary>
/// Configuration of a site to build:
///
/// - FileSystems
/// - Plugins
/// - Defines
/// - LoggerFactory
/// </summary>
public class SiteConfiguration : ISiteLoggerProvider
{
    private readonly List<Type> _pluginTypes;

    public SiteConfiguration(SiteLoggerFactory loggerFactory = null)
    {
        LogEventId = 1;
        _pluginTypes = new List<Type>();
        Defines = new List<string>();
        FileSystems = new SiteFileSystems();
        LoggerFactory = loggerFactory ?? new SiteLoggerFactory();
        CommandRunners = new List<ISiteCommandRunner>();
        Log = LoggerFactory.CreateLogger("lunet");
        ShouldRunGarbageCollectorOnReload = true;
        SharedCache = new ConcurrentDictionary<object, object>();
    }

    public SiteFileSystems FileSystems { get; }

    public List<string> Defines { get; }

    public SiteLoggerFactory LoggerFactory { get; }

    public ILogger Log { get; }
        
    public int LogEventId { get; set; }

    public List<Type> PluginTypes() => new List<Type>(_pluginTypes);

    public List<ISiteCommandRunner> CommandRunners { get; }

    // TODO: make this configurable from the command line
    public bool ShouldRunGarbageCollectorOnReload { get; set; }
        
    public bool SingleThreaded { get; set; }
        
    public bool ShowStacktraceOnError { get; set; }

    public IProfiler Profiler { get; set; }

    public ConcurrentDictionary<object, object> SharedCache { get; }

    public SiteConfiguration RegisterPlugin<TPlugin>() where TPlugin : ISitePlugin
    {
        RegisterPlugin(typeof(TPlugin));
        return this;
    }

    public SiteConfiguration RegisterPlugin(Type pluginType)
    {
        if (pluginType == null) throw new ArgumentNullException(nameof(pluginType));
        if (_pluginTypes.Contains(pluginType)) return this;

        if (!typeof(ISitePlugin).GetTypeInfo().IsAssignableFrom(pluginType))
        {
            throw new ArgumentException("Expecting a plugin type inheriting from ISitePlugin", nameof(pluginType));
        }

        _pluginTypes.Add(pluginType);
        return this;
    }
}