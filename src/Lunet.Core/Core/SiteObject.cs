
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
using Zio;
using Zio.FileSystems;

namespace Lunet.Core
{
    public class SiteObject : DynamicObject
    {
        public const string MetaFolderName = "_meta";
        public const string SharedFolderName = "shared";
        public const string OutputFolderName = "www";
        public const string TempFolderName = ".lunet";
        public const string DefaultPageExtensionValue = ".html";
        public const string DefaultConfigFileName = "config.scriban";

        private bool _isInitialized;
        private readonly AggregateFileSystem _fileSystem;
        private readonly List<IFileSystem> _contentFileSystems;
        private IFileSystem _tempFileSystem;

        public SiteObject(ILoggerFactory loggerFactory, IEnumerable<Func<ISitePlugin>> pluginFactory = null)
        {
            var sharedFolder = Path.Combine(Path.GetDirectoryName(typeof(SiteObject).GetTypeInfo().Assembly.Location), SharedFolderName);

            _contentFileSystems = new List<IFileSystem>();

            var sharedPhysicalFileSystem = new PhysicalFileSystem();
            SharedFileSystem = new SubFileSystem(sharedPhysicalFileSystem, sharedPhysicalFileSystem.ConvertPathFromInternal(sharedFolder));

            _fileSystem = new AggregateFileSystem(SharedFileSystem);

            ConfigFile = new FileEntry(_fileSystem, UPath.Root / DefaultConfigFileName);

            var metaPath = UPath.Root / MetaFolderName;

            MetaFileSystem = new SubFileSystem(_fileSystem, metaPath);

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
        //        OutputFolder = PrivateBaseFolder / OutputFolderName;
        //    }
        //}

        public IFileSystem SharedFileSystem { get; }

        public IFileSystem InputFileSystem { get; set; }

        public IFileSystem TempFileSystem
        {
            get => _tempFileSystem;
            set
            {
                _tempFileSystem = value;
                TempMetaFileSystem = _tempFileSystem == null ? new SubFileSystem(_tempFileSystem, UPath.Root / MetaFolderName) : null;
            }
        }

        public IFileSystem FileSystem => _fileSystem;

        public IFileSystem OutputFileSystem { get; set; }

        public IFileSystem TempMetaFileSystem { get; private set; }

        public IFileSystem MetaFileSystem { get; }

        public OrderedList<Func<ISitePlugin>> Plugins { get; }

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

        public void AddContentFileSystem(IFileSystem fileSystem)
        {
            if (!_contentFileSystems.Contains(fileSystem))
            {
                _contentFileSystems.Add(fileSystem);
            }

            _fileSystem.SetFileSystems(_contentFileSystems);
            _fileSystem.AddFileSystem(InputFileSystem);
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
                TempFileSystem.DeleteDirectory(UPath.Root / MetaFolderName, true);
                this.Info($"Directory {UPath.Root / MetaFolderName} deleted");
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
            throw new NotImplementedException();
            //if (BaseFolder.Info.Exists && BaseFolder.Info.GetFileSystemInfos().Length != 0 && !force)
            //{
            //    this.Error($"The directory [{BaseFolder.FullName}] is not empty. Use the --force option to force the creation of an empty website");
            //    return;
            //}
            //FolderInfo sourceNewSite = Path.Combine(SharedMetaFolder, "newsite");
            //FolderInfo destinationDir = BaseFolder;

            //sourceNewSite.CopyTo(destinationDir, true, false);

            //this.Info($"New website created at {destinationDir}");
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