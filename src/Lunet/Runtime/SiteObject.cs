// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lunet.Helpers;
using Lunet.Plugins;
using Lunet.Scripts;
using Lunet.Statistics;
using Lunet.Themes;
using Microsoft.Extensions.Logging;
using Scriban.Parsing;

namespace Lunet.Runtime
{
    public class SiteObject : LunetObject
    {
        private const string SiteDirectoryName = "_site";
        private const string DefaultPageExtensionValue = ".html";
        private readonly Stopwatch clock;

        public SiteObject(string configFilePathArg, ILoggerFactory loggerFactory = null)
        {
            if (string.IsNullOrEmpty(configFilePathArg)) throw new ArgumentNullException(nameof(configFilePathArg));

            // Make sure to get a proper config file path
            this.ConfigFile = new FileInfo(configFilePathArg).FullName;
            var baseDirectoryFullpath = Path.GetDirectoryName(ConfigFile);
            if (baseDirectoryFullpath == null)
            {
                throw new ArgumentException($"Cannot find parent directory of config file [{ConfigFile}]");
            }
            this.BaseDirectoryInfo = new DirectoryInfo(baseDirectoryFullpath);
            BaseDirectory = this.BaseDirectoryInfo.FullName;

            clock = new Stopwatch();

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

            Statistics = new SiteStatistics();

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

        public SiteStatistics Statistics { get; }

        public bool UrlAsFile
        {
            get { return DynamicObject.GetSafeValue<bool>(SiteVariables.UrlAsFile); }
            set { DynamicObject[SiteVariables.UrlAsFile] = value; }
        }

        public string DefaultPageExtension
        {
            get { return DynamicObject.GetSafeValue<string>(SiteVariables.DefaultPageExtension); }
            set { DynamicObject[SiteVariables.DefaultPageExtension] = value; }
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

        public void Generate()
        {
            Generator.Run();
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
                if (entry.Name == SiteFactory.DefaultConfigFilename)
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
                    clock.Restart();
                    LoadPage(rootDirectory, (FileInfo)entry, out page);
                    clock.Stop();
                    if (page != null)
                    {
                        // Update statistics
                        Statistics.GetContentStat(page).LoadingParsingTime += clock.Elapsed;

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
                clock.Restart();
                if (Scripts.TryRunFrontMatter(page.Script, page.DynamicObject))
                {
                    clock.Stop();
                    Pages.Add(page);

                    // Update statistics
                    Statistics.GetContentStat(page).EvaluateTime += clock.Elapsed;
                }
            }

            // Process the index
            if (indexPage != null)
            {
                clock.Restart();
                if (Scripts.TryRunFrontMatter(indexPage.Script, indexPage.DynamicObject))
                {
                    clock.Stop();
                    Pages.Add(indexPage);

                    // Update statistics
                    Statistics.GetContentStat(indexPage).EvaluateTime += clock.Elapsed;
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