
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Lunet.Helpers;
using Lunet.Scripts;
using Lunet.Statistics;
using Microsoft.Extensions.Logging;
using Scriban.Parsing;

namespace Lunet.Core
{
    public class SiteObject : DynamicObject
    {
        public const string MetaFolderName = "_meta";
        public const string SharedFolderName = "shared";
        public const string OutputFolderName = "www";
        public const string PrivateFolderName = ".lunet";
        public const string DefaultPageExtensionValue = ".html";
        public const string DefaultConfigFileName = "config.sban";

        private readonly Stopwatch clock;
        private FolderInfo _baseFolder;
        private bool isInitialized;

        public SiteObject() : this(null)
        {
        }

        public SiteObject(ILoggerFactory loggerFactory, IEnumerable<Func<ISitePlugin>> pluginFactory = null)
        {
            // Initialize by default with current directory
            BaseFolder = ".";

            SharedFolder = Path.Combine(Path.GetDirectoryName(typeof(SiteObject).GetTypeInfo().Assembly.Location), SharedFolderName);
            SharedMetaFolder = Path.Combine(SharedFolder, MetaFolderName);

            ContentProviders = new OrderedList<IContentProvider>();
            clock = new Stopwatch();

            StaticFiles = new PageCollection();
            Pages = new PageCollection();
            DynamicPages = new PageCollection();
            
            // Create the logger
            LoggerFactory = loggerFactory ?? new LoggerFactory();
            LoggerFactory.AddProvider(new LoggerProviderIntercept(this));
            Log = LoggerFactory.CreateLogger("lunet");
            ContentTypes = new ContentTypeManager();

            DefaultPageExtension = DefaultPageExtensionValue;

            Html = new HtmlObject(this);
            SetValue(SiteVariables.Html, Html, true);

            CommandLine = new LunetCommandLine(this);

            Statistics = new SiteStatistics();

            Scripts = new ScriptingPlugin(this);

            Content = new ContentPlugin(this);

            Plugins = new OrderedList<Func<ISitePlugin>>();
            if (pluginFactory != null)
            {
                Plugins.AddRange(pluginFactory);
            }
        }

        public FileInfo ConfigFile { get; private set; }

        /// <summary>
        /// Gets or sets the base directory of the website (input files, config file)
        /// </summary>
        public FolderInfo BaseFolder
        {
            get { return _baseFolder; }

            set
            {
                // Update all 
                _baseFolder = value;
                PrivateBaseFolder = Path.Combine(BaseFolder.FullName, PrivateFolderName);
                MetaFolder = BaseFolder.GetSubFolder(MetaFolderName);
                PrivateMetaFolder = PrivateBaseFolder.GetSubFolder(MetaFolderName);
                ConfigFile = new FileInfo(Path.Combine(BaseFolder, DefaultConfigFileName));
                OutputFolder = PrivateBaseFolder.GetSubFolder(OutputFolderName);
            }
        }

        public OrderedList<Func<ISitePlugin>> Plugins { get; }

        public FolderInfo PrivateBaseFolder { get; private set; }

        public FolderInfo MetaFolder { get; private set; }

        public FolderInfo SharedFolder { get; }

        public FolderInfo SharedMetaFolder { get; }

        public FolderInfo PrivateMetaFolder { get; private set; }

        public FolderInfo OutputFolder { get; set; }

        /// <summary>
        /// Gets the logger factory that was used to create the site logger <see cref="Log"/>.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the site logger.
        /// </summary>
        public ILogger Log { get; }

        public PageCollection StaticFiles { get; }

        public PageCollection DynamicPages { get; }

        public PageCollection Pages { get; }

        public bool HasErrors { get; set; }

        public ContentPlugin Content { get; }

        public ScriptingPlugin Scripts { get; }

        public SiteStatistics Statistics { get; }

        public ContentTypeManager ContentTypes { get; }

        public LunetCommandLine CommandLine { get; }

        public HtmlObject Html { get; }

        /// <summary>
        /// An event occured when the site has been updated.
        /// </summary>
        public event EventHandler<EventArgs> Updated;

        public string BasePath
        {
            get { return this.GetSafeValue<string>(SiteVariables.BasePath); }
            set { this[SiteVariables.BasePath] = value; }
        }

        public string BaseUrl
        {
            get { return this.GetSafeValue<string>(SiteVariables.BaseUrl); }
            set { this[SiteVariables.BaseUrl] = value; }
        }

        public bool BaseUrlForce
        {
            get { return this.GetSafeValue<bool>(SiteVariables.BaseUrlForce); }
            set { this[SiteVariables.BaseUrlForce] = value; }
        }

        public bool UrlAsFile
        {
            get { return GetSafeValue<bool>(SiteVariables.UrlAsFile); }
            set { this[SiteVariables.UrlAsFile] = value; }
        }

        public string DefaultPageExtension
        {
            get { return GetSafeValue<string>(SiteVariables.DefaultPageExtension); }
            set { this[SiteVariables.DefaultPageExtension] = value; }
        }

        public OrderedList<IContentProvider> ContentProviders { get; }
        
        public IEnumerable<FolderInfo> ContentFolders
        {
            get
            {
                // The site input directory will override any existing content (from extend or builtin)
                yield return BaseFolder;

                foreach (var contentProvider in ContentProviders)
                {
                    foreach (var dir in contentProvider.GetFolders())
                    {
                        yield return dir;
                    }
                }

                yield return SharedFolder;
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
            if (ConfigFile.Exists)
            {
                PrivateBaseFolder.Delete();
                this.Info($"Directory {PrivateBaseFolder} deleted");
                return 0;
            }

            this.Error($"The config file [{ConfigFile.Name}] was not found");
            return 1;
        }

        public IEnumerable<FolderInfo> MetaFolders
        {
            get
            {
                foreach (var directory in ContentFolders)
                {
                    yield return directory.GetSubFolder(MetaFolderName);
                }
            }
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
            Scripts.TryImportScriptStatement(defineStatement, this, ScriptFlags.AllowSiteFunctions);
        }

        public void Create(bool force)
        {
            if (BaseFolder.Info.Exists && BaseFolder.Info.GetFileSystemInfos().Length != 0 && !force)
            {
                this.Error($"The directory [{BaseFolder.FullName}] is not empty. Use the --force option to force the creation of an empty website");
                return;
            }
            FolderInfo sourceNewSite = Path.Combine(SharedMetaFolder, "newsite");
            FolderInfo destinationDir = BaseFolder;

            sourceNewSite.CopyTo(destinationDir, true, false);

            this.Info($"New website created at {destinationDir}");
        }

        public bool Initialize()
        {
            if (ConfigFile.Exists)
            {
                if (isInitialized)
                {
                    return true;
                }

                isInitialized = true;
                 
                // We then actually load the config
                return Scripts.TryImportScriptFromFile(ConfigFile.FullName, this,
                    ScriptFlags.Expect | ScriptFlags.AllowSiteFunctions);
            }

            this.Error($"The config file [{ConfigFile.Name}] was not found");
            return false;
        }

        public void Build()
        {
            if (Initialize())
            {
                Content.Run();
            }
        }

        public int Run(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            try
            {
                foreach (var pluginFactory in Plugins)
                {
                    pluginFactory();
                }

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
        }

        private class LoggerProviderIntercept : ILoggerProvider
        {
            private readonly SiteObject site;

            public LoggerProviderIntercept(SiteObject site)
            {
                this.site = site;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new LoggerIntercept(site);
            }
        }

        private class LoggerIntercept : ILogger
        {
            private readonly SiteObject site;

            public LoggerIntercept(SiteObject site)
            {
                this.site = site;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
                {
                    site.HasErrors = true;
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