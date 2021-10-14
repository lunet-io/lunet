﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Blake3;
using Lunet.Helpers;
using Lunet.Scripts;
using Scriban.Parsing;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Core
{
    public class ContentPlugin : SitePlugin
    {
        /// <summary>
        /// The lock for tracking related dictionaries
        /// </summary>
        private readonly object _lockForTracking = new object();
        private readonly HashSet<DirectoryEntry> _trackingPreviousOutputDirectories;
        private readonly HashSet<FileEntry> _trackingPreviousOutputFiles;
        private readonly Dictionary<FileEntry, FileEntry> _trackingFilesWritten;
        private const string SharedCacheContentKey = "content-cache-key";
        
        private readonly Dictionary<string, PageCollection> _mapTypeToPages;
        private bool _isInitialized;
        private readonly Stopwatch _totalDuration;

        public ContentPlugin(SiteObject site) : base(site)
        {
            _trackingPreviousOutputDirectories = new HashSet<DirectoryEntry>();
            _trackingPreviousOutputFiles = new HashSet<FileEntry>();
            Scripts = Site.Scripts;
            _trackingFilesWritten = new Dictionary<FileEntry, FileEntry>();
            _mapTypeToPages = new Dictionary<string, PageCollection>();
            _totalDuration = new Stopwatch();
            
            // Setup layout types
            LayoutTypes = new ContentLayoutTypes();
            Site.SetValue(SiteVariables.LayoutTypes, LayoutTypes, true);

            BeforeInitializingProcessors = new OrderedList<ISiteProcessor>();
            BeforeLoadingProcessors = new OrderedList<ISiteProcessor>();
            BeforeLoadingContentProcessors = new OrderedList<TryProcessPreContentDelegate>();
            AfterLoadingProcessors = new OrderedList<ISiteProcessor>();
            AfterRunningProcessors = new OrderedList<IContentProcessor>();
            BeforeProcessingProcessors = new OrderedList<ISiteProcessor>();
            ContentProcessors = new OrderedList<IContentProcessor>();
            AfterProcessingProcessors = new OrderedList<ISiteProcessor>();
            
            Finder = new PageFinderProcessor(this);
            AfterLoadingProcessors.Add(Finder); // Make uid available before running Markdown content
        }

        private ScriptingPlugin Scripts { get; }
        
        
        public ContentLayoutTypes LayoutTypes { get; }

        public OrderedList<ISiteProcessor> BeforeInitializingProcessors { get; }

        public OrderedList<ISiteProcessor> BeforeLoadingProcessors { get; }

        public OrderedList<TryProcessPreContentDelegate> BeforeLoadingContentProcessors { get; }

        public OrderedList<ISiteProcessor> AfterLoadingProcessors { get; }

        public OrderedList<IContentProcessor> AfterRunningProcessors { get; }
        
        public OrderedList<ISiteProcessor> BeforeProcessingProcessors { get; }

        public OrderedList<IContentProcessor> ContentProcessors { get; }
        
        public OrderedList<ISiteProcessor> AfterProcessingProcessors { get; }
        
        public PageFinderProcessor Finder { get; }

        private int MaxDegreeOfParallelism => Site.Config.SingleThreaded ? 1 : DataflowBlockOptions.Unbounded;

        /// <summary>
        /// Determines whether if the layout type is a list layout.
        /// </summary>
        /// <param name="layoutType">Type of the layout.</param>
        /// <returns><c>true</c> if the specified layout type is a list layout; otherwise, <c>false</c>.</returns>
        public static bool IsListLayout(string layoutType)
        {
            // NOTE: this is not pluggable today, but it should be simple enough to avoid complicated setup
            return (layoutType != null && (layoutType.EndsWith("s") || layoutType.EndsWith("list")));
        }
        
        private void Initialize()
        {
            _totalDuration.Restart();

            // We collect all previous file entries in the output directory
            CollectPreviousFileEntries();

            _trackingFilesWritten.Clear();
            Site.Statistics.Reset();

            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;
        }

        public void PreInitialize()
        {
            // Before loading content
            TryRunProcess(BeforeInitializingProcessors, ProcessingStage.BeforeInitializing);
        }

        public void Run()
        {
            Initialize();

            try
            {
                // If we have any errors, early exit
                if (Site.HasErrors)
                {
                    return;
                }

                // Make sure that all collections are cleared before building the content
                Site.StaticFiles.Clear();
                Site.Pages.Clear();
                Site.DynamicPages.Clear();
                
                // Before loading content
                if (!TryRunProcess(BeforeLoadingProcessors, ProcessingStage.BeforeLoadingContent))
                {
                    // If we have any errors during processing, early exit
                    return;
                }

                // Load content
                if (!LoadAllContent())
                {
                    return;
                }

                // Run all content
                RunAllContent();

                // Before processing content
                if (!TryRunProcess(BeforeProcessingProcessors, ProcessingStage.BeforeProcessingContent))
                {
                    // If we have any errors during processing, early exit
                    return;
                }

                // Reset content to process
                ResetPagesPerLayoutType();

                // Collect all contents per layout type
                // Process static content files
                CollectPagesPerLayoutType(Site.StaticFiles);
                // Process pages content (files with front matter)
                CollectPagesPerLayoutType(Site.Pages);
                // Process dynamic pages content (files with front matter)
                CollectPagesPerLayoutType(Site.DynamicPages);
                
                // Process content per layout type
                ProcessPagesPerLayoutType();

                // End processing content
                TryRunProcess(AfterProcessingProcessors, ProcessingStage.AfterProcessingContent);

                // Remove output files
                CleanupOutputFiles();
            }
            finally
            {
                // Update statistics
                _totalDuration.Stop();
                Site.Statistics.TotalTime = _totalDuration.Elapsed;
            }
        }

        // Used only to create a shared instance for hash
        private readonly ConcurrentDictionary<UPath, Blake3.Hash> _cacheHashInstance = new ConcurrentDictionary<UPath, Hash>();

        public bool TryCopyContentToOutput(ContentObject fromFile, UPath outputPath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            outputPath.AssertAbsolute();

            var clock = Stopwatch.StartNew();

            var outputFile = new FileEntry(Site.OutputFileSystem, outputPath);
            var outputDir = outputFile.Directory;
            if (outputDir == null)
            {
                throw new ArgumentException("Output directory cannot be empty", nameof(outputPath));
            }
            TrackDestination(outputFile, fromFile.SourceFile);

            var stat = Site.Statistics.GetContentStat(fromFile);

            CreateDirectory(outputDir);

            try
            {
                // If the file has a content, we will use this instead
                if (fromFile.Content != null)
                {
                    // Use a site wide cache for content hash
                    if (!Site.Config.SharedCache.TryGetValue(SharedCacheContentKey, out var values) || !(values is ConcurrentDictionary<UPath, Blake3.Hash> mapPathToHash))
                    {
                        mapPathToHash = (ConcurrentDictionary<UPath, Blake3.Hash>) Site.Config.SharedCache.GetOrAdd(SharedCacheContentKey, _cacheHashInstance);
                    }

                    // Don't output the file if the hash hasn't changed
                    HashUtil.Blake3HashString(fromFile.Content, out var contentHash);
                    if (!mapPathToHash.TryGetValue(outputPath, out var previousHash) || contentHash != previousHash)
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Write file [{outputFile}]");
                        }

                        using (var writer = new StreamWriter(outputFile.Open(FileMode.Create, FileAccess.Write)))
                        {
                            writer.Write(fromFile.Content);
                            writer.Flush();

                            // Update statistics
                            stat.Static = false;
                            stat.OutputBytes += writer.BaseStream.Length;
                        }

                        // Store the new content hash
                        mapPathToHash[outputPath] = contentHash;
                    }
                }
                // If the source file is not newer than the destination file, don't overwrite it
                else if (fromFile.SourceFile != null && (!outputFile.Exists || (fromFile.ModifiedTime > outputFile.LastWriteTime)))
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{outputFile}]");
                    }

                    // If the output file is readonly, make sure that it is not readonly before overwriting
                    if (outputFile.Exists && (outputFile.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        outputFile.Attributes = outputFile.Attributes & ~FileAttributes.ReadOnly;
                    }

                    fromFile.SourceFile.CopyTo(outputFile, true);

                    // Don't copy readonly attributes for output folder
                    outputFile.Attributes = fromFile.SourceFile.Attributes & ~FileAttributes.ReadOnly;

                    // Update statistics
                    stat.Static = true;
                    stat.OutputBytes += fromFile.Length;
                }
            }
            catch (Exception ex)
            {
                Site.Error(ex, fromFile.SourceFile != null
                    ? $"Unable to copy file [{fromFile.SourceFile}] to [{outputFile}]. Reason:{ex.GetReason()}"
                    : $"Unable to copy file to [{outputFile}]. Reason:{ex.GetReason()}");
                return false;
            }
            finally
            {
                stat.OutputTime += clock.Elapsed;
            }

            return true;
        }

        public void TrackDestination(FileEntry outputFile, FileEntry sourceFile)
        {
            if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));

            lock (_lockForTracking)
            {
                if (sourceFile != null)
                {
                    FileEntry previousSourceFile;
                    if (_trackingFilesWritten.TryGetValue(outputFile, out previousSourceFile))
                    {
                        Site.Error($"The content [{previousSourceFile}] and [{sourceFile}] have the same Url output [{sourceFile}]");
                    }
                    else
                    {
                        _trackingFilesWritten.Add(outputFile, sourceFile);
                    }
                }

                // If the directory is used for a new file, remove it from the list of previous directories
                // Note that we remove even if things are not working after, so that previous files are kept 
                // in case of an error
                var previousDir = outputFile.Directory;
                while (previousDir != null)
                {
                    _trackingPreviousOutputDirectories.Remove(previousDir);
                    previousDir = previousDir.Parent;
                }

                _trackingPreviousOutputFiles.Remove(outputFile);
            }
        }

        private bool TryRunProcess(IEnumerable<ISiteProcessor> processors, ProcessingStage stage)
        {
            var statistics = Site.Statistics;

            // Callback plugins once files have been initialized but not yet processed
            int i = 0;
            var clock = Stopwatch.StartNew();
            foreach (var processor in processors)
            {
                var stat = statistics.GetPluginStat(processor);
                stat.Order = i;

                clock.Restart();
                try
                {
                    processor.Process(stage);
                }
                catch (Exception exception)
                {
                    if (exception is LunetException)
                    {
                        Site.Error(exception, $"Unexpected error in processor {processor.Name}. Reason: {exception.Message}");
                    }
                    else
                    {
                        Site.Error(exception, $"Unexpected error in processor {processor.Name}. Reason: {exception.Message}");
                    }
                    return false;
                }
                finally
                {
                    clock.Stop();
                    stat.BeginProcessTime = clock.Elapsed;
                }
                i++;
            }

            return true;
        }

        private void RunPage(ContentObject page)
        {
            // Update statistics
            var evalClock = Stopwatch.StartNew();
            var contentStat = Site.Statistics.GetContentStat(page);

            if (page.Script != null && TryRunPageWithScript(page))
            {
                page.InitializeAfterRun();

                // If page is discarded, skip it
                if (page.Discard)
                {
                    return;
                }
            }

            var pendingPageProcessors = new OrderedList<IContentProcessor>();
            TryProcessPage(page, ContentProcessingStage.Running, AfterRunningProcessors, pendingPageProcessors, false);

            contentStat.RunningTime += evalClock.Elapsed;
        }

        public void ProcessPages(PageCollection pages, bool copyOutput)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));
            
            // Process the content of all pages
            var pageActionBlock = new ActionBlock<ContentObject>(page =>
                {
                    var pendingPageProcessors = new OrderedList<IContentProcessor>();
                    TryProcessPage(page, ContentProcessingStage.Processing, ContentProcessors, pendingPageProcessors, copyOutput);
                },
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = false,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                }
            );

            // Process pages in order of their layout types
            foreach (var page in pages)
            {
                pageActionBlock.Post(page);
            }
            
            pageActionBlock.Complete();
            pageActionBlock.Completion.Wait();
        }

        private void ResetPagesPerLayoutType()
        {
            // Don't remove the PageCollection (to keep them allocated around)
            foreach (var mapTypeToPage in _mapTypeToPages)
            {
                mapTypeToPage.Value.Clear();
            }
        }

        private void CollectPagesPerLayoutType(PageCollection pages)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));

            // Collect and group pages per their layout type
            foreach (var page in pages)
            {
                var layoutType = page.LayoutType ?? ContentLayoutTypes.Single;
                if (!_mapTypeToPages.TryGetValue(layoutType, out var subPages))
                {
                    subPages = new PageCollection();
                    _mapTypeToPages.Add(layoutType, subPages);
                }
                subPages.Add(page);
            }
        }

        private void ProcessPagesPerLayoutType()
        {
            var layoutTypes = _mapTypeToPages.Keys.OrderBy(x => LayoutTypes.GetSafeValue<int>(x)).ToList();
            foreach (var layoutType in layoutTypes)
            {
                var pages = _mapTypeToPages[layoutType];
                ProcessPages(pages, true);
            }
        }

        private bool LoadAllContent()
        {
            Site.StaticFiles.Clear();
            Site.Pages.Clear();

            // Load a content asynchronously
            var contentLoaderBlock = new TransformBlock<(FileEntry, int), ContentObject>(
                reference =>
                {
                    var content = LoadContent(reference.Item1);
                    // We transfer the weight to the content
                    if (content[PageVariables.Weight] == null)
                    {
                        content.Weight = reference.Item2;
                    }
                    return content;
                },
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = false,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                });

            // Add the content loaded to the correct list (static vs pages)
            var contentAdderBlock = new ActionBlock<ContentObject>(content =>
            {
                // We don't need lock as this block has a MaxDegreeOfParallelism = 1
                var list = content.Script != null ? Site.Pages : Site.StaticFiles;
                list.Add(content);
            });

            // Link loader and adder
            contentLoaderBlock.LinkTo(contentAdderBlock, new DataflowLinkOptions() { PropagateCompletion = true });

            // We don't use a block for this part (annoying without struct tuples)

            // Compute the list of files that we will actually process
            var directories = new Queue<DirectoryEntry>();
            directories.Enqueue(new DirectoryEntry(Site.FileSystem, UPath.Root));
            while (directories.Count > 0)
            {
                var nextDirectory = directories.Dequeue();
                // The weight is the order in the directory by name
                int weight = 10;
                foreach (var contentReference in LoadDirectory(nextDirectory, directories).OrderBy(x => x.Name))
                {
                    contentLoaderBlock.Post((contentReference, weight));
                    weight += 10;
                }
            }

            // We are done loading content, wait for completion
            contentLoaderBlock.Complete();
            contentAdderBlock.Completion.Wait();

            // Right after we have been loading the content, we can iterate on all content
            if (!TryRunProcess(AfterLoadingProcessors, ProcessingStage.AfterLoadingContent))
            {
                // If we have any errors during processing, early exit
                return false;
            }

            return true;
        }
        
        private void RunAllContent()
        {
            // Uncomment to dispatch on multithread
            //const int MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded;
            const int MaxDegreeOfParallelism = 1;

            // Run script content asynchronously
            var contentRunnerBlock = new ActionBlock<ContentObject>(
                RunPage,
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = false,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                });

            foreach (var page in Site.Pages)
            {
                contentRunnerBlock.Post(page);
            }
            foreach (var page in Site.StaticFiles)
            {
                contentRunnerBlock.Post(page);
            }

            contentRunnerBlock.Complete();
            contentRunnerBlock.Completion.Wait();

            // Finally, we sort pages by natural order
            Site.Pages.Sort();
        }

        private IEnumerable<FileEntry> LoadDirectory(DirectoryEntry directory, Queue<DirectoryEntry> directoryQueue)
        {
            foreach (var entry in directory.EnumerateEntries())
            {
                if (!Site.IsHandlingPath(entry.Path))
                {
                    continue;
                }

                if (entry is FileEntry fileEntry)
                {
                    // Optimization, if an entry is coming from the aggregate, let's try to return the entry from the underlying system directly
                    if (fileEntry.FileSystem is AggregateFileSystem aggregateFs)
                    {
                        yield return (FileEntry) aggregateFs.FindFirstFileSystemEntry(entry.Path);
                    }
                    else
                    {
                        yield return (FileEntry) entry;
                    }
                }
                else if (entry.Name != SiteFileSystems.LunetFolderName )
                {
                    directoryQueue.Enqueue((DirectoryEntry)entry);
                }
            }
        }

        private ContentObject LoadContent(FileEntry file)
        {
            ContentObject page = null;
            Span<byte> buffer = stackalloc byte[16];

            var clock = Stopwatch.StartNew();
            var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try
            {
                var count = stream.Read(buffer);
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

                var newBuffer = buffer.Slice(startFrontMatter);
                foreach (var frontParser in Scripts.FrontMatterParsers)
                {
                    if (frontParser.CanHandle(newBuffer))
                    {
                        for (int i = startFrontMatter + 3; i < count; i++)
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

                        break;
                    }
                }

                // Run pre-content (e.g AttributesPlugin)
                ScriptObject preContent = null;
                foreach (var preLoadingContentProcessor in BeforeLoadingContentProcessors)
                {
                    preLoadingContentProcessor(file.Path, ref preContent);
                }

                if (hasFrontMatter)
                {
                    page = LoadPageScript(Site, stream, file, preContent);
                    stream = null;
                }
                else
                {

                    page = new FileContentObject(Site, file, preContent: preContent);
                    
                    //// Run pre-processing on static content as well
                    //var pendingPageProcessors = new OrderedList<IContentProcessor>();
                    //TryProcessPage(page, ContentProcessingStage.AfterLoading, AfterLoadingProcessors, pendingPageProcessors, false);
                }

                // Initialize the page loaded
                page?.Initialize();
            }
            finally
            {
                // Dispose stream used
                stream?.Dispose();
            }

            clock.Stop();
            Site.Statistics.GetContentStat(page).LoadingParsingTime += clock.Elapsed;

            return page;
        }
        private static ContentObject LoadPageScript(SiteObject site, Stream stream, FileEntry file, ScriptObject preContent)
        {
            var evalClock = Stopwatch.StartNew();
            // Read the stream
            var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            // Early dispose the stream
            stream.Dispose();

            ContentObject page = null;

            // Parse the page, using front-matter mode
            var scriptInstance = site.Scripts.ParseScript(content, file.FullName, ScriptMode.FrontMatterAndContent);
            if (!scriptInstance.HasErrors)
            {
                page = new FileContentObject(site, file, scriptInstance, preContent: preContent);
            }
            evalClock.Stop();

            // Update statistics
            var contentStat = site.Statistics.GetContentStat(page);
            contentStat.LoadingTime += evalClock.Elapsed;

            return page;
        }

        private bool TryProcessPage(ContentObject page, ContentProcessingStage stage, IEnumerable<IContentProcessor> pageProcessors, OrderedList<IContentProcessor> pendingPageProcessors, bool copyOutput)
        {
            // If page is discarded, skip it
            if (page.Discard)
            {
                return false;
            }

            // By default working on all processors
            // Order is important!
            pendingPageProcessors.AddRange(pageProcessors);
            bool hasBeenProcessed = true;
            bool breakProcessing = false;

            // We process the page going through all IContentProcessor from the end of the list
            // (more priority) to the beginning of the list (less priority).
            // An IContentProcessor can transform the page to another type of content
            // that could then be processed by another IContentProcessor
            // But we make sure that a processor cannot process a page more than one time
            // to avoid an infinite loop
            var clock = Stopwatch.StartNew();
            while (hasBeenProcessed && !breakProcessing && !page.Discard)
            {
                hasBeenProcessed = false;
                for (int i = pendingPageProcessors.Count - 1; i >= 0; i--)
                {
                    var processor = pendingPageProcessors[i];

                    // Note that page.ContentType can be changed by a processor 
                    // while processing a page
                    clock.Restart();
                    try
                    {
                        var result = processor.TryProcessContent(page, stage);

                        if (result != ContentResult.None)
                        {
                            // Update statistics per plugin
                            var statistics = Site.Statistics;
                            var stat = statistics.GetPluginStat(processor);
                            stat.PageCount++;
                            stat.ContentProcessTime += clock.Elapsed;

                            hasBeenProcessed = true;
                            pendingPageProcessors.RemoveAt(i);
                            breakProcessing = result == ContentResult.Break;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is LunetException lunetEx)
                        {
                            Site.Error(ex, $"Error while processing {page.Path} by {processor.Name} processor. {lunetEx.Message}");
                        }
                        else
                        {
                            Site.Error(ex, $"Error while processing {page.Path} by {processor.Name} processor.");
                        }
                        breakProcessing = true;
                        hasBeenProcessed = true;
                        break;
                    }
                }
            }
            pendingPageProcessors.Clear();

            // Copy only if the file are marked as include
            if (copyOutput && !breakProcessing && !page.Discard)
            {
                Site.Content.TryCopyContentToOutput(page, page.GetDestinationPath());
            }

            return true;
        }

        private bool TryRunPageWithScript(ContentObject page)
        {
            page.ScriptObjectLocal ??= new ScriptObject();
            return Site.Scripts.TryEvaluatePage(page, page.Script, page.SourceFile.Path, page);
        }

        public void CreateDirectory(DirectoryEntry directory)
        {
            if (!directory.Exists)
            {
                if (Site.CanTrace())
                {
                    Site.Trace($"Create directory [{directory}]");
                }

                try
                {
                    directory.Create();
                }
                catch (Exception ex)
                {
                    Site.Error(ex, $"Unable to create directory [{directory}]. Reason:{ex.GetReason()}");
                }
            }
        }

        private void CleanupOutputFiles()
        {
            // Remove all previous files that have not been generated
            foreach (var outputFile in _trackingPreviousOutputFiles)
            {
                try
                {
                    if (outputFile.Exists)
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete file [{outputFile}]");
                        }
                        outputFile.Delete();
                    }
                }
                catch (Exception)
                {
                }
            }

            foreach (var outputDirectory in _trackingPreviousOutputDirectories)
            {
                try
                {
                    if (outputDirectory.Exists)
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete directory [{outputDirectory}]");
                        }
                        outputDirectory.Delete(true);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void CollectPreviousFileEntries()
        {
            var outputDirectoryInfo = new DirectoryEntry(Site.OutputFileSystem, UPath.Root);
            _trackingPreviousOutputDirectories.Clear();
            _trackingPreviousOutputFiles.Clear();

            if (!outputDirectoryInfo.Exists)
            {
                return;
            }

            var directories = new Queue<DirectoryEntry>();
            directories.Enqueue(outputDirectoryInfo);

            while (directories.Count > 0)
            {
                var nextDirectory = directories.Dequeue();

                if (!Equals(nextDirectory, outputDirectoryInfo))
                {
                    _trackingPreviousOutputDirectories.Add(nextDirectory);
                }

                foreach (var entry in nextDirectory.EnumerateEntries())
                {
                    if (entry is FileEntry)
                    {
                        _trackingPreviousOutputFiles.Add(((FileEntry)entry));
                    }
                    else if (entry is DirectoryEntry)
                    {
                        directories.Enqueue((DirectoryEntry)entry);
                    }
                }
            }
        }
    }
}