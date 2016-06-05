// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Lunet.Bundles;
using Lunet.Helpers;
using Lunet.Hosting;
using Lunet.Plugins;
using Lunet.Scripts;
using Lunet.Statistics;
using Microsoft.Extensions.Logging;
using Scriban.Parsing;

namespace Lunet.Core
{
    public class SiteObject : DynamicObject
    {
        public const string MetaDirectoryName = "_meta";
        private const string SharedFilesName = "data";
        private const string OutputDirectoryName = "www";
        private const string PrivateDirectoryName = ".lunet";
        private const string DefaultPageExtensionValue = ".html";
        private const string DefaultConfigFileName = "config.sban";

        private readonly Stopwatch clock;
        private readonly Dictionary<Type, ISiteService> services;
        internal readonly OrderedList<ISiteService> orderedServices;
        private FolderInfo baseDirectory;
        private bool isInitialized;

        public SiteObject() : this(null)
        {
        }

        public SiteObject(ILoggerFactory loggerFactory)
        {
            // Initialize by default with current directory
            BaseDirectory = ".";

            BuiltinDirectory = Path.Combine(Path.GetDirectoryName(typeof(SiteObject).GetTypeInfo().Assembly.Location), SharedFilesName);
            BuiltinMetaDirectory = Path.Combine(BuiltinDirectory, MetaDirectoryName);

            services = new Dictionary<Type, ISiteService>();
            orderedServices = new OrderedList<ISiteService>();
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

            Watcher = new SiteWatcher(this);

            CommandLine = new LunetCommandLine(this);

            Statistics = new SiteStatistics();

            Scripts = new ScriptService(this);
            Register(Scripts);

            Builder = new SiteBuilder(this);
            Register(Builder);

            Plugins = new PluginService(this);
            Register(Plugins);

            Plugins.ImportPluginsFromAssembly(typeof(SiteObject).GetTypeInfo().Assembly);
        }

        public FileInfo ConfigFile { get; private set; }

        /// <summary>
        /// Gets or sets the base directory of the website (input files, config file)
        /// </summary>
        public FolderInfo BaseDirectory
        {
            get { return baseDirectory; }

            set
            {
                // Update all 
                baseDirectory = value;
                PrivateBaseDirectory = Path.Combine(BaseDirectory.FullName, PrivateDirectoryName);
                MetaDirectory = BaseDirectory.GetSubFolder(MetaDirectoryName);
                PrivateMetaDirectory = PrivateBaseDirectory.GetSubFolder(MetaDirectoryName);
                ConfigFile = new FileInfo(Path.Combine(BaseDirectory, DefaultConfigFileName));
                OutputDirectory = PrivateBaseDirectory.GetSubFolder(OutputDirectoryName);
            }
        }

        public FolderInfo PrivateBaseDirectory { get; private set; }

        public FolderInfo MetaDirectory { get; private set; }

        public FolderInfo BuiltinDirectory { get; }

        public FolderInfo BuiltinMetaDirectory { get; }

        public FolderInfo PrivateMetaDirectory { get; private set; }

        public FolderInfo OutputDirectory { get; set; }

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

        public PluginService Plugins { get; }

        public SiteBuilder Builder { get; }

        public ScriptService Scripts { get; }

        public SiteStatistics Statistics { get; }

        public ContentTypeManager ContentTypes { get; }

        public LunetCommandLine CommandLine { get; }

        public HtmlObject Html { get; }

        public SiteWatcher Watcher { get; }

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
        
        public IEnumerable<FolderInfo> ContentDirectories
        {
            get
            {
                // The site input directory will override any existing content (from extend or builtin)
                yield return BaseDirectory;

                foreach (var contentProvider in ContentProviders)
                {
                    foreach (var dir in contentProvider.GetDirectories())
                    {
                        yield return dir;
                    }
                }

                yield return BuiltinDirectory;
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
                FileUtil.DeleteDirectory(PrivateBaseDirectory);
                this.Info($"Directory {PrivateBaseDirectory} deleted");
                return 0;
            }

            this.Error($"The config file [{ConfigFile.Name}] was not found");
            return 1;
        }

        public IEnumerable<FolderInfo> MetaDirectories
        {
            get
            {
                foreach (var directory in ContentDirectories)
                {
                    yield return directory.GetSubFolder(MetaDirectoryName);
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
            if (BaseDirectory.Info.Exists && BaseDirectory.Info.GetFileSystemInfos().Length != 0 && !force)
            {
                this.Error($"The directory [{BaseDirectory.FullName}] is not empty. Use the --force option to force the creation of an empty website");
                return;
            }
            FolderInfo sourceNewSite = Path.Combine(BuiltinMetaDirectory, "newsite");
            FolderInfo destinationDir = BaseDirectory;

            FileUtil.DirectoryCopy(sourceNewSite, destinationDir, true, false);

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
                Builder.Run();
            }
        }

        public void WatchForRebuild(Action<SiteObject> rebuild)
        {
            if (rebuild == null) throw new ArgumentNullException(nameof(rebuild));
            Watcher.Start();

            Watcher.FileSystemEvents += (sender, args) =>
            {
                if (this.CanTrace())
                {
                    this.Trace($"Received file events [{args.FileEvents.Count}]");
                }

                try
                {
                    // Regenerate website
                    // NOTE: we are recreating a full new SiteObject here (not incremental)
                    var siteObject = SiteFactory.FromDirectory(BaseDirectory, LoggerFactory);

                    rebuild(siteObject);
                }
                catch (Exception ex)
                {
                    this.Error($"Unexpected error while reloading the site. Reason: {ex.GetReason()}");
                }
            };
        }

        public int Run(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            try
            {
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

        public T GetService<T>() where T : class, ISiteService
        {
            ISiteService instance;
            services.TryGetValue(typeof(T), out instance);
            return (T)instance;
        }

        public void Register<T>(T instance) where T : class, ISiteService
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            RegisterAs<T, T>(instance);
        }

        public void RegisterAs<T, TInterface>(T instance) where T : class, TInterface where TInterface: ISiteService
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            if (!services.ContainsKey(typeof(TInterface)))
            {
                services[typeof(TInterface)] = instance;
                if (!orderedServices.Contains(instance))
                {
                    orderedServices.Add(instance);
                }
            }
        }

        public void Load()
        {
            StaticFiles.Clear();
            Pages.Clear();

            // Get the list of root directories from themes
            var rootDirectories = new List<FolderInfo>(ContentDirectories);

            // Compute the list of files that we will actually process
            var filesLoaded = new HashSet<string>();
            foreach (var rootDirectory in rootDirectories)
            {
                var directories = new Queue<DirectoryInfo>();
                directories.Enqueue(rootDirectory);
                while (directories.Count > 0)
                {
                    var nextDirectory = directories.Dequeue();
                    LoadDirectory(rootDirectory, nextDirectory, directories, filesLoaded);
                }
            }

            // Sort pages by natural order
            Pages.Sort();
        }

        private void LoadPage(DirectoryInfo rootDirectory, FileInfo file, out ContentObject page)
        {
            page = null;
            var buffer = new byte[16];

            var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                // Rewind to 0
                stream.Position = 0;

                bool hasFrontMatter = false;
                bool isBinary = false;

                int startFrontMatter = 0;

                // Does it start with UTF8 BOM? If yes, skip it
                // EF BB BF
                if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                {
                    startFrontMatter = 3;
                }

                if (buffer[startFrontMatter] == '{' && buffer[startFrontMatter + 1] == '{')
                {
                    for (int i = startFrontMatter + 2; i < count; i++)
                    {
                        if (buffer[i] == 0)
                        {
                            isBinary = true;
                            break;
                        }
                    }

                    if (!isBinary)
                    {
                        hasFrontMatter = true;
                    }
                }

                if (hasFrontMatter)
                {
                    page = LoadPageScript(stream, rootDirectory, file);
                    stream = null;
                }
                else
                {
                    this.StaticFiles.Add(new ContentObject(this, rootDirectory, file));
                }
            }
            finally
            {
                // Dispose stream used
                stream?.Dispose();
            }
        }

        private ContentObject LoadPageScript(Stream stream, DirectoryInfo rootDirectory, FileInfo file)
        {
            // Read the stream
            var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            // Early dispose the stream
            stream.Dispose();

            ContentObject page = null;

            // Parse the page, using front-matter mode
            var scriptPage = Scripts.ParseScript(content, file.FullName, ScriptMode.FrontMatter);
            if (!scriptPage.HasErrors)
            {
                page = new ContentObject(this, rootDirectory, file)
                {
                    Script = scriptPage.Page
                };

                var evalClock = Stopwatch.StartNew();
                if (Builder.TryPreparePage(page))
                {
                    evalClock.Stop();

                    // Update statistics
                    var contentStat = Statistics.GetContentStat(page);
                    
                    contentStat.EvaluateTime += evalClock.Elapsed;

                    // Update the summary of the page
                    evalClock.Restart();
                    SummaryHelper.UpdateSummary(page);
                    evalClock.Stop();

                    // Update statistics
                    contentStat.SummaryTime += evalClock.Elapsed;
                }
            }

            return page;
        }

        private void LoadDirectory(FolderInfo rootDirectory, DirectoryInfo directory, Queue<DirectoryInfo> directoryQueue, HashSet<string> loaded)
        {
            var pages = new List<ContentObject>();
            foreach (var entry in directory.EnumerateFileSystemInfos())
            {
                if (entry.Name == SiteFactory.DefaultConfigFilename)
                {
                    continue;
                }

                if (entry is FileInfo)
                {
                    // If the relative path is already registered, we won't process this file
                    var relativePath = rootDirectory.GetRelativePath(entry.FullName, PathFlags.Normalize);
                    if (loaded.Contains(relativePath))
                    {
                        continue;
                    }
                    loaded.Add(relativePath);

                    ContentObject page;
                    clock.Restart();
                    LoadPage(rootDirectory, (FileInfo)entry, out page);
                    clock.Stop();

                    if (page != null)
                    {
                        // Update statistics
                        Statistics.GetContentStat(page).LoadingParsingTime += clock.Elapsed;
                        Pages.Add(page);
                    }
                }
                else if (!entry.Name.StartsWith("_") && entry.Name != PrivateDirectoryName)
                {
                    directoryQueue.Enqueue((DirectoryInfo)entry);
                }
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