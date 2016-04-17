﻿using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Helpers;
using Lunet.Layouts;
using Lunet.Plugins;
using Lunet.Runtime;
using Lunet.Scripts;
using Lunet.Themes;
using Microsoft.Extensions.Logging;
using Textamina.Scriban.Parsing;

namespace Lunet
{
    public class SiteObject : LunetObject
    {
        internal const string DefaultConfigFileName1 = "config.sban";

        private const string SiteDirectoryName = "_site";
        private const string DefaultPageExtensionValue = ".html";

        private SiteObject(string configFilePathArg, ILoggerFactory loggerFactory)
        {
            if (configFilePathArg == null) throw new ArgumentNullException(nameof(configFilePathArg));

            // Make sure to get a proper config file path
            this.ConfigFile = new FileInfo(configFilePathArg).FullName;
            var baseDirectoryFullpath = Path.GetDirectoryName(ConfigFile);
            if (baseDirectoryFullpath == null)
            {
                throw new ArgumentException($"Cannot find parent directory of config file [{ConfigFile}]");
            }
            this.BaseDirectoryInfo = new DirectoryInfo(baseDirectoryFullpath);
            BaseDirectory = this.BaseDirectoryInfo.FullName;

            StaticFiles = new List<ContentObject>();
            Pages = new List<ContentObject>();

            // Plugins

            // LoadConfig the plugins after they have been loaded/modified from the config
            Managers = new OrderedList<ManagerBase>()
            {
                (Meta = new MetaManager(this)),
                (Themes = new ThemeManager(this)),
                (Plugins = new PluginManager(this))
            };

            // Create the logger
            LoggerFactory = loggerFactory ?? new LoggerFactory();
            LoggerFactory.AddProvider(new LoggerProviderIntercept(this));
            Log = LoggerFactory.CreateLogger("lunet");

            OutputDirectory = this.GetSubDirectory(SiteDirectoryName).FullName;

            DefaultPageExtension = DefaultPageExtensionValue;

            Scripts = new ScriptManager(this);

            // Must be last
            Generator = new SiteGenerator(this);
        }

        public string ConfigFile { get; }

        public DirectoryInfo BaseDirectoryInfo { get; }

        public string BaseDirectory { get; }

        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets the logger factory that was used to create the site logger <see cref="Log"/>.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the site logger.
        /// </summary>
        public ILogger Log { get; }

        public List<ContentObject> StaticFiles { get; }

        public List<ContentObject> Pages { get; }

        public bool HasErrors { get; set; }

        internal List<ManagerBase> Managers { get; }

        public MetaManager Meta { get; }

        public PluginManager Plugins { get; }

        public ThemeManager Themes { get; }

        public SiteGenerator Generator { get; }

        public ScriptManager Scripts { get; }

        public bool UrlAsFile
        {
            get { return GetSafe<bool>(SiteVariables.UrlAsFile); }
            set { this[SiteVariables.UrlAsFile] = value; }
        }

        public string DefaultPageExtension
        {
            get { return GetSafe<string>(SiteVariables.DefaultPageExtension); }
            set { this[SiteVariables.DefaultPageExtension] = value; }
        }

        public IEnumerable<DirectoryInfo> ContentDirectories
        {
            get
            {
                yield return BaseDirectoryInfo;

                foreach (var theme in Themes.CurrentList)
                {
                    yield return theme.DirectoryInfo;
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

        public void Load()
        {
            StaticFiles.Clear();
            Pages.Clear();

            // Get the list of root directories from themes
            var rootDirectories = new List<DirectoryInfo>(ContentDirectories);

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
        }

        /// <summary>
        /// Gets the <see cref="SiteObject"/> from the specified configuration file path.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <returns>The <see cref="SiteObject"/></returns>
        /// <exception cref="System.IO.FileNotFoundException">If the <paramref name="configFilePath"/> file does not exist.</exception>
        public static SiteObject FromFile(string configFilePath)
        {
            return FromFile(configFilePath, null);
        }

        /// <summary>
        /// Gets the <see cref="SiteObject"/> from the specified configuration file path.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>The <see cref="SiteObject"/></returns>
        /// <exception cref="System.IO.FileNotFoundException">If the <paramref name="configFilePath"/> file does not exist.</exception>
        public static SiteObject FromFile(string configFilePath, ILoggerFactory loggerFactory)
        {
            var site = TryFromFile(configFilePath, loggerFactory);
            if (site == null)
            {
                throw new FileNotFoundException($"The config file [{configFilePath}] is not a valid path", configFilePath);
            }
            return site;
        }

        /// <summary>
        /// Gets the <see cref="SiteObject"/> from the specified directory or any parent directories.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>The <see cref="SiteObject"/></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public static SiteObject FindFromDirectory(string directoryPath, ILoggerFactory loggerFactory = null)
        {
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
            var directory = new DirectoryInfo(directoryPath);
            while (directory != null)
            {
                var site = TryFromFile(Path.Combine(directory.FullName, DefaultConfigFileName1), loggerFactory);
                           
                if (site != null)
                {
                    return site;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"The config file [{DefaultConfigFileName1}] was not found from the directory path and up [{directoryPath}]");
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
                    // Read the stream
                    var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    // Early dispose the stream
                    stream.Dispose();
                    stream = null;

                    // Parse the page, using front-matter mode
                    var scriptPage = Scripts.ParseScript(content, file.FullName, ParsingMode.FrontMatter);
                    if (!scriptPage.HasErrors)
                    {
                        page = new ContentObject(rootDirectory, file, this) { Script = scriptPage.Page };
                    }
                }
                else
                {
                    this.StaticFiles.Add(new ContentObject(rootDirectory, file, this));
                }
            }
            finally
            {
                // Dispose stream used
                stream?.Dispose();
            }
        }

        private void LoadDirectory(DirectoryInfo rootDirectory, DirectoryInfo directory, Queue<DirectoryInfo> directoryQueue, HashSet<string> loaded)
        {
            var pages = new List<ContentObject>();
            ContentObject indexPage = null;
            foreach (var entry in directory.EnumerateFileSystemInfos())
            {
                if (entry.Name == DefaultConfigFileName1)
                {
                    continue;
                }

                if (entry is FileInfo)
                {
                    // If the relative path is already registered, we won't process this file
                    var relativePath = PathUtil.GetRelativePath(rootDirectory.FullName, entry.FullName, true);
                    if (loaded.Contains(relativePath))
                    {
                        continue;
                    }
                    loaded.Add(relativePath);

                    ContentObject page;
                    LoadPage(rootDirectory, (FileInfo)entry, out page);
                    if (page != null)
                    {
                        if (page.SourceFileInfo.Name.StartsWith("index.") && indexPage == null)
                        {
                            indexPage = page;
                        }
                        else
                        {
                            pages.Add(page);
                        }
                    }
                }
                else if (!entry.Name.StartsWith("_"))
                {
                    directoryQueue.Enqueue((DirectoryInfo)entry);
                }
            }

            // Process all pages before the index
            foreach (var page in pages)
            {
                if (Scripts.TryRunFrontMatter(page.Script, page))
                {
                    Pages.Add(page);
                }
            }

            // Process the index
            if (indexPage != null)
            {
                if (Scripts.TryRunFrontMatter(indexPage.Script, indexPage))
                {
                    Pages.Add(indexPage);
                }
            }
        }

        private static SiteObject TryFromFile(string configFilePath, ILoggerFactory loggerFactory)
        {
            if (configFilePath == null) throw new ArgumentNullException(nameof(configFilePath));
            if (!File.Exists(configFilePath))
            {
                return null;
            }
            var site = new SiteObject(configFilePath, loggerFactory);
            return site;
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