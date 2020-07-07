
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Lunet.Helpers;
using Lunet.Scripts;
using Lunet.Statistics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Scriban.Parsing;
using Zio;
using Zio.FileSystems;

namespace Lunet.Core
{
    public class SiteObject : DynamicObject
    {
        public const string TempSiteFolderName = "tmp";
        public static readonly UPath TempSiteFolder = UPath.Root / TempSiteFolderName;

        public const string SharedFolderName = "shared";
        public static readonly UPath SiteFolder = UPath.Root / SharedFolderName;

        public const string LunetFolderName = ".lunet";
        public static readonly UPath LunetFolder = UPath.Root / LunetFolderName;

        public const string BuildFolderName = "build";
        public static readonly UPath BuildFolder = LunetFolder / BuildFolderName;

        public const string DefaultOutputFolderName = "www";
        public const string DefaultPageExtensionValue = ".html";
        public const string DefaultConfigFileName = "config.scriban";

        private bool _isInitialized;
        private readonly AggregateFileSystem _fileSystem;
        private readonly List<IFileSystem> _contentFileSystems;
        private IFileSystem _siteFileSystem;
        private bool _pluginInitialized;
        private readonly ContainerBuilder _pluginBuilders;

        public SiteObject(ILoggerFactory loggerFactory = null)
        {
            ErrorRedirect = "/404.html";
            var sharedFolder = Path.Combine(Path.GetDirectoryName(typeof(SiteObject).GetTypeInfo().Assembly.Location), SharedFolderName);

            _contentFileSystems = new List<IFileSystem>();
            var sharedPhysicalFileSystem = new PhysicalFileSystem();

            // Make sure that SharedFileSystem is a read-only filesystem
            SharedFileSystem = new ReadOnlyFileSystem(new SubFileSystem(sharedPhysicalFileSystem, sharedPhysicalFileSystem.ConvertPathFromInternal(sharedFolder)));
            SharedMetaFileSystem = SharedFileSystem.GetOrCreateSubFileSystem(LunetFolder);

            _fileSystem = new AggregateFileSystem(SharedFileSystem);

            MetaFileSystem = new SubFileSystem(_fileSystem, LunetFolder);

            ConfigFile = new FileEntry(_fileSystem, UPath.Root / DefaultConfigFileName);

            StaticFiles = new PageCollection();
            Pages = new PageCollection();
            DynamicPages = new PageCollection();
            
            // Create the logger
            LoggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new LoggerProviderIntercept(this))
                    .AddFilter(LogFilter)
                    .AddConsole();
            });
            
            Log = LoggerFactory.CreateLogger("lunet");
            ContentTypes = new ContentTypeManager();

            DefaultPageExtension = DefaultPageExtensionValue;

            Html = new HtmlPage(this);

            CommandLine = new LunetCommandLine(this);

            Statistics = new SiteStatistics();

            Scripts = new ScriptingPlugin(this);

            Content = new ContentPlugin(this);

            Plugins = new OrderedList<ISitePlugin>();

            Helpers = new HelperObject(this);

            _pluginBuilders = new ContainerBuilder();
            _pluginBuilders.RegisterInstance(LoggerFactory).As<ILoggerFactory>();
            _pluginBuilders.RegisterInstance(this);
        }

        public FileEntry ConfigFile { get; }

        ///// <summary>
        ///// Gets or sets the base directory of the website (input files, config file)
        ///// </summary>
        //public UPath BaseFolder
        //{
        //    get { return _baseFolder; }

        //    set
        //    {
        //        // Update all 
        //        _baseFolder = value;
        //        PrivateBaseFolder = Path.Combine(BaseFolder.FullName, TempFolderName);
        //        MetaFolder = BaseFolder / MetaFolderName;
        //        PrivateMetaFolder = PrivateBaseFolder / MetaFolderName;
        //        ConfigFile = BaseFolder / DefaultConfigFileName;
        //        OutputFolder = PrivateBaseFolder / DefaultOutputFolderName;
        //    }
        //}

        public IFileSystem SharedFileSystem { get; }
        
        public void Setup(string inputDirectory, string outputDirectory, params string[] defines)
        {
            var rootFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, inputDirectory ?? "."));

            var diskfs = new PhysicalFileSystem();

            var siteFileSystem = new SubFileSystem(diskfs, diskfs.ConvertPathFromInternal(rootFolder));
            SiteFileSystem = siteFileSystem;

            // Add defines
            foreach (var value in defines)
            {
                AddDefine(value);
            }

            var outputFolder = outputDirectory != null
                ? Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, outputDirectory))
                : Path.Combine(rootFolder, LunetFolderName + "/build/" + DefaultOutputFolderName);

            var outputFolderForFs = diskfs.ConvertPathFromInternal(outputFolder);
            OutputFileSystem = diskfs.GetOrCreateSubFileSystem(outputFolderForFs);
        }

        public IFileSystem SiteFileSystem
        {
            get => _siteFileSystem;
            set
            {
                _siteFileSystem = value;
                TempSiteFileSystem = _siteFileSystem?.GetOrCreateSubFileSystem(LunetFolder / BuildFolderName / TempSiteFolderName);
                TempMetaFileSystem = _siteFileSystem?.GetOrCreateSubFileSystem(LunetFolder / BuildFolderName / TempSiteFolderName / LunetFolderName);
                UpdateFileSystem();
            }
        }
        
        public IFileSystem TempSiteFileSystem { get; private set; }

        public IFileSystem FileSystem => _fileSystem;

        public HelperObject Helpers { get; }

        public IFileSystem OutputFileSystem { get; set; }

        public IFileSystem SharedMetaFileSystem { get; }

        public IFileSystem TempMetaFileSystem { get; private set; }

        public IFileSystem MetaFileSystem { get; private set; }

        public OrderedList<ISitePlugin> Plugins { get; }

        /// <summary>
        /// Gets the logger factory that was used to create the site logger <see cref="Log"/>.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the site logger.
        /// </summary>
        public ILogger Log { get; }
        
        internal int LogEventId { get; set; }

        public PageCollection StaticFiles { get; }

        public PageCollection DynamicPages { get; }

        public PageCollection Pages { get; }

        public bool HasErrors { get; set; }

        public ContentPlugin Content { get; }

        public ScriptingPlugin Scripts { get; }

        public SiteStatistics Statistics { get; }

        public ContentTypeManager ContentTypes { get; }

        public LunetCommandLine CommandLine { get; }

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

        public void AddContentFileSystem(IFileSystem fileSystem)
        {
            if (!_contentFileSystems.Contains(fileSystem))
            {
                _contentFileSystems.Add(fileSystem);
            }
            UpdateFileSystem();
        }

        private void UpdateFileSystem()
        {
            _fileSystem.ClearFileSystems();
            if (TempSiteFileSystem != null)
            {
                _fileSystem.AddFileSystem(TempSiteFileSystem);
            }
            foreach (var contentfs in _contentFileSystems)
            {
                _fileSystem.AddFileSystem(contentfs);
            }
            if (SiteFileSystem != null)
            {
                _fileSystem.AddFileSystem(SiteFileSystem);
            }
        }

        public bool LogFilter(string category, LogLevel level)
        {
            var levelStr = Scripts.SiteFunctions.LogObject.GetSafeValue<string>("level")?.ToLowerInvariant() ?? "info";
            var filterLevel = LogLevel.Information;
            switch (levelStr)
            {
                case "debug":
                    filterLevel = LogLevel.Debug;
                    break;
                case "info":
                    filterLevel = LogLevel.Information;
                    break;
                case "error":
                    filterLevel = LogLevel.Error;
                    break;
                case "trace":
                    filterLevel = LogLevel.Trace;
                    break;
                case "critical":
                    filterLevel = LogLevel.Critical;
                    break;
                case "warning":
                    filterLevel = LogLevel.Warning;
                    break;
            }

            return level >= filterLevel;
        }

        public int Clean()
        {
            if (!ConfigFile.Exists)
            {
                SiteFileSystem.DeleteDirectory(BuildFolder, true);
                this.Info($"Directory {BuildFolder} deleted");
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

        public void Create(bool force)
        {
            if (!force && SiteFileSystem.EnumerateFiles(UPath.Root).Any())
            {
                this.Error($"The destination directory is not empty. Use the --force option to force the creation of an empty website");
                return;
            }

            SharedMetaFileSystem.CopyDirectory("/newsite", SiteFileSystem,UPath.Root, true);

            // TODO: Add created at "folder"
            this.Info($"New website created.");
        }

        public SiteObject Register<TPlugin>() where TPlugin : ISitePlugin
        {
            Register(typeof(TPlugin));
            return this;
        }

        public SiteObject Register(Type pluginType)
        {
            if (pluginType == null) throw new ArgumentNullException(nameof(pluginType));
            if (!typeof(ISitePlugin).GetTypeInfo().IsAssignableFrom(pluginType))
            {
                throw new ArgumentException("Expecting a plugin type inheriting from ISitePlugin", nameof(pluginType));
            }
            _pluginBuilders.RegisterType(pluginType).As<ISitePlugin>().AsSelf().SingleInstance();
            return this;
        }

        public SiteObject Clone()
        {
            var siteObject = new SiteObject(LoggerFactory);
            foreach (var plugin in Plugins)
            {
                siteObject.Register(plugin.GetType());
            }

            siteObject.LogEventId = LogEventId;
            siteObject.SiteFileSystem = SiteFileSystem;
            siteObject.OutputFileSystem = OutputFileSystem;
            siteObject.InitializePlugins();

            return siteObject;
        }

        public bool Initialize()
        {
            if (ConfigFile.Exists)
            {
                if (_isInitialized)
                {
                    return true;
                }

                _isInitialized = true;

                object result;
                // We then actually load the config
                return Scripts.TryImportScriptFromFile(ConfigFile, this, ScriptFlags.Expect | ScriptFlags.AllowSiteFunctions, out result);
            }

            this.Error($"The config file [{ConfigFile.Name}] was not found");
            return false;
        }

        public void Build()
        {
            var clock = Stopwatch.StartNew();
            InitializePlugins();

            // Pre-initialize stage (running before we initialize site object)
            Content.PreInitialize();

            if (Initialize())
            {
                Content.Run();
            }
            clock.Stop();
            var elapsed = clock.Elapsed.TotalMilliseconds;

            if (this.CanInfo())
            {
                this.Info($"Build finished in {elapsed}ms. {Statistics.GetSummary()}");
            }
        }

        private void InitializePlugins()
        {
            if (!_pluginInitialized)
            {
                var container = _pluginBuilders.Build();
                var plugins = container.Resolve<IEnumerable<ISitePlugin>>().ToList();
                Plugins.AddRange(plugins);
                _pluginInitialized = true;
            }
        }

        public int Run(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            try
            {
                InitializePlugins();

                if (ConfigFile.Exists)
                {
                    Initialize();
                }

                return CommandLine.Execute(args);
            }
            catch (Exception ex)
            {
                this.Error($"Unexpected error. Reason: {ex.GetReason()}");
                return 1;
            }
            finally
            {
                // Force a dispose to make sure the ConsoleLogger is flushed
                LoggerFactory.Dispose();
            }
        }

        private class LoggerProviderIntercept : ILoggerProvider
        {
            private readonly SiteObject _site;

            public LoggerProviderIntercept(SiteObject site)
            {
                this._site = site;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new LoggerIntercept(_site);
            }
        }

        private class LoggerIntercept : ILogger
        {
            private readonly SiteObject _site;

            public LoggerIntercept(SiteObject site)
            {
                this._site = site;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
                {
                    _site.HasErrors = true;
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
    }
}