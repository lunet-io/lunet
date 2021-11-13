// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autofac;
using Lunet.Helpers;
using Lunet.Scripts;
using Lunet.Statistics;
using Microsoft.Extensions.Logging;
using Zio;

namespace Lunet.Core;

public class SiteObject : DynamicObject, ISiteLoggerProvider
{
    public const string DefaultPageExtensionValue = ".html";

    private bool _isInitialized;
    private bool _pluginInitialized;
    private readonly List<Type> _pluginTypes;

    public SiteObject(SiteConfiguration configuration)
    {
        Config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _pluginTypes = Config.PluginTypes();
        ReadmeAsIndex = true;
        ErrorRedirect = "/404.html";

        SharedFileSystem = Config.FileSystems.SharedFileSystem;
        SiteFileSystem = Config.FileSystems.InputFileSystem;
        CacheSiteFileSystem = Config.FileSystems.CacheSiteFileSystem;
        FileSystem = Config.FileSystems.FileSystem;
        OutputFileSystem = Config.FileSystems.OutputFileSystem;
        SharedMetaFileSystem = Config.FileSystems.SharedMetaFileSystem;
        CacheMetaFileSystem = Config.FileSystems.CacheMetaFileSystem;
        MetaFileSystem = Config.FileSystems.MetaFileSystem;
        ConfigFile = Config.FileSystems.ConfigFile;

        Scripts = new ScriptingPlugin(this);

        Builtins = new BuiltinsObject(this);
        Builtins.Initialize();

        LoggerFactory.LogFilter = LogFilter;

        StaticFiles = new PageCollection();
        Pages = new PageCollection();
        DynamicPages = new PageCollection();

        ContentTypes = new ContentTypeManager();

        DefaultPageExtension = DefaultPageExtensionValue;

        Html = new HtmlPage(this);

        Statistics = new SiteStatistics();
            
        Content = new ContentPlugin(this);

        Plugins = new OrderedList<ISitePlugin>();

        ForceExcludes = new GlobCollection()
        {
            $"**/{SiteFileSystems.LunetFolderName}/{SiteFileSystems.BuildFolderName}/**",
            $"/{SiteFileSystems.DefaultConfigFileName}",
        };
        Excludes = new GlobCollection()
        {
            "**/~*/**",
            "**/.*/**",
            "**/_*/**",
        };
        Includes = new GlobCollection()
        {
            $"**/{SiteFileSystems.LunetFolderName}/**",
        };
        SetValue(SiteVariables.ForceExcludes, ForceExcludes, true);
        SetValue(SiteVariables.Excludes, Excludes, true);
        SetValue(SiteVariables.Includes, Includes, true);
        SetValue(SiteVariables.Pages, Pages, true);
        Environment = "prod";

        foreach (var define in Config.Defines)
        {
            AddDefine(define);
        }
    }

    public SiteConfiguration Config { get; }

    public FileEntry ConfigFile { get; }

    public GlobCollection ForceExcludes { get; }

    public GlobCollection Excludes { get;  }

    public GlobCollection Includes { get; }

    /// <summary>
    /// Checks if the specified path is included or excluded
    /// </summary>
    public bool IsHandlingPath(UPath path)
    {
        if (path.IsNull) return false;

        // e.g always exclude .lunet/build, can't be overriden by the users
        var isForceExcluded = ForceExcludes.IsMatch(path);
        if (isForceExcluded) return false;

        // If we have an explicit include, it overrides any excludes
        var isIncluded = Includes.IsMatch(path);
        if (isIncluded) return true;

        var isExcluded = Excludes.IsMatch(path);
        return !isExcluded;
    }

    public IFileSystem SharedFileSystem { get; }

    public IFileSystem SiteFileSystem { get; }

    public IFileSystem CacheSiteFileSystem { get; }

    public IFileSystem FileSystem { get; }

    public IFileSystem OutputFileSystem { get; }

    public IFileSystem SharedMetaFileSystem { get; }

    public IFileSystem CacheMetaFileSystem { get; }

    public IFileSystem MetaFileSystem { get; }

    public BuiltinsObject Builtins { get; }

    public OrderedList<ISitePlugin> Plugins { get; }

    /// <summary>
    /// Gets the logger factory that was used to create the site logger <see cref="Log"/>.
    /// </summary>
    public SiteLoggerFactory LoggerFactory => Config.LoggerFactory;

    /// <summary>
    /// Gets the site logger.
    /// </summary>
    public ILogger Log => Config.Log;

    public bool ShowStacktraceOnError
    {
        get => Config.ShowStacktraceOnError;
        set => Config.ShowStacktraceOnError = value;
    }

    public int LogEventId
    {
        get => Config.LogEventId;
        set => Config.LogEventId = value;
    }

    public PageCollection StaticFiles { get; }

    public PageCollection DynamicPages { get; }

    public PageCollection Pages { get; }

    public bool HasErrors { get; set; }

    public ContentPlugin Content { get; }

    public ScriptingPlugin Scripts { get; }

    public SiteStatistics Statistics { get; }

    public ContentTypeManager ContentTypes { get; }

    public HtmlPage Html { get; }

    public string BasePath
    {
        get => GetSafeValue<string>(SiteVariables.BasePath);
        set => this[SiteVariables.BasePath] = value;
    }

    public string BaseUrl
    {
        get => GetSafeValue<string>(SiteVariables.BaseUrl);
        set => this[SiteVariables.BaseUrl] = value;
    }

    public bool BaseUrlForce
    {
        get => GetSafeValue<bool>(SiteVariables.BaseUrlForce);
        set => this[SiteVariables.BaseUrlForce] = value;
    }

    public bool UrlAsFile
    {
        get => GetSafeValue<bool>(SiteVariables.UrlAsFile);
        set => this[SiteVariables.UrlAsFile] = value;
    }

    public string DefaultPageExtension
    {
        get => GetSafeValue<string>(SiteVariables.DefaultPageExtension);
        set => this[SiteVariables.DefaultPageExtension] = value;
    }

    public string ErrorRedirect
    {
        get => GetSafeValue<string>(SiteVariables.ErrorRedirect);
        set => this[SiteVariables.ErrorRedirect] = value;
    }

    public bool ReadmeAsIndex
    {
        get => GetSafeValue<bool>(SiteVariables.ReadmeAsIndex);
        set => this[SiteVariables.ReadmeAsIndex] = value;
    }
        
    public string Environment
    {
        get => GetSafeValue<string>(SiteVariables.Environment);
        set => SetValue(SiteVariables.Environment, value);
    }

    public string Layout
    {
        get => GetSafeValue<string>(SiteVariables.Layout);
        set => SetValue(SiteVariables.Layout, value);
    }
        
    private bool LogFilter(string category, LogLevel level)
    {
        var levelStr = Builtins.LogObject.GetSafeValue<string>("level")?.ToLowerInvariant() ?? "info";
        var filterLevel = LogLevel.Information;
        switch (levelStr)
        {
            case "trace":
                filterLevel = LogLevel.Trace;
                break;
            case "debug":
                filterLevel = LogLevel.Debug;
                break;
            case "info":
                filterLevel = LogLevel.Information;
                break;
            case "warning":
                filterLevel = LogLevel.Warning;
                break;
            case "error":
                filterLevel = LogLevel.Error;
                break;
            case "critical":
                filterLevel = LogLevel.Critical;
                break;
        }

        return level >= filterLevel;
    }

    public int Clean()
    {
        if (!ConfigFile.Exists)
        {
            SiteFileSystem.DeleteDirectory(SiteFileSystems.BuildFolder, true);
            this.Info($"Directory {SiteFileSystems.BuildFolder} deleted");
            return 0;
        }

        this.Error($"The config file [{ConfigFile.Name}] was not found");
        return 1;
    }

    public string GetSafeDefaultPageExtension()
    {
        var extension = DefaultPageExtension;
        if (extension == ".html" || extension == ".htm")
        {
            return extension;
        }
        this.Warning($"Invalid [site.{SiteVariables.DefaultPageExtension} = \"{extension}\"]. Expecting only .html or htm. Reset to [{DefaultPageExtensionValue}]");
        DefaultPageExtension = DefaultPageExtensionValue;
        return DefaultPageExtensionValue;
    }


    public void AddDefine(string defineStatement)
    {
        if (defineStatement == null) throw new ArgumentNullException(nameof(defineStatement));
        object result;
        Scripts.TryImportScriptStatement(defineStatement, this, ScriptFlags.AllowSiteFunctions, out result);
    }

    public int Create(bool force)
    {
        if (!force && SiteFileSystem.EnumerateFiles(UPath.Root).Any())
        {
            this.Error($"The destination directory is not empty. Use the --force option to force the creation of an empty website");
            return 1;
        }

        SharedMetaFileSystem.CopyDirectory("/new/site", SiteFileSystem,UPath.Root, true, false);

        this.Info($"New website created at `{SiteFileSystem.ConvertPathToInternal(UPath.Root)}`.");
        return 0;
    }

    public SiteObject Clone()
    {
        var siteObject = new SiteObject(Config);
        siteObject.InitializePlugins();
        return siteObject;
    }

    public bool Initialize()
    {
        InitializePlugins();

        if (ConfigFile.Exists)
        {
            if (_isInitialized)
            {
                return true;
            }

            // Pre-initialize stage (running before we initialize site object)
            Content.PreInitialize();

            _isInitialized = true;

            // We then actually load the config
            return Scripts.TryImportScriptFromFile(ConfigFile, this, ScriptFlags.Expect | ScriptFlags.AllowSiteFunctions, out _);
        }

        this.Error($"The config file [{ConfigFile.Name}] was not found");
        return false;
    }

    public void Build()
    {
        if (this.CanInfo())
        {
            this.Info($"Site build started (environment: {Environment})");
        }

        var clock = Stopwatch.StartNew();
        if (Initialize())
        {
            Content.Run();
        }
        clock.Stop();
        var elapsed = clock.Elapsed.TotalMilliseconds;

        if (this.CanInfo())
        {
            this.Info($"Site build finished in {elapsed}ms. {Statistics.GetSummary()}");
        }
    }

    public void AddContentFileSystem(IFileSystem contentFileSystem)
    {
        Config.FileSystems.AddContentFileSystem(contentFileSystem);
    }

    private void InitializePlugins()
    {
        if (!_pluginInitialized)
        {
            var pluginBuilders = new ContainerBuilder();
            pluginBuilders.RegisterInstance(LoggerFactory).As<ILoggerFactory>();
            pluginBuilders.RegisterInstance(this);
            foreach (var pluginType in _pluginTypes)
            {
                pluginBuilders.RegisterType(pluginType).As<ISitePlugin>().AsSelf().SingleInstance();
            }
            var container = pluginBuilders.Build();

            var plugins = container.Resolve<IEnumerable<ISitePlugin>>().ToList();
            Plugins.AddRange(plugins);
            _pluginInitialized = true;
        }
    }
}