// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lunet.Helpers;
using Lunet.Plugins;
using Lunet.Scripts;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Core
{
    public class PluginCollection<T> : OrderedList<T> where T : ISitePluginCore
    {
        private readonly SiteObject site;

        public PluginCollection(SiteObject site)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            this.site = site;
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            Initialize(item);
        }


        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
            Initialize(item);
        }

        protected virtual T Initialize(T item)
        {
            var clock = Stopwatch.StartNew();
            item.Initialize(site);
            site.Statistics.GetPluginStat(item).InitializeTime += clock.Elapsed;
            return item;
        }
    }

    public class SiteBuilder : ServiceBase
    {
        private readonly HashSet<string> previousOutputDirectories;
        private readonly HashSet<string> previousOutputFiles;
        private readonly Dictionary<string, FileInfo> filesWritten;
        private bool isInitialized;
        private readonly Stopwatch totalDuration;

        public SiteBuilder(SiteObject site) : base(site)
        {
            previousOutputDirectories = new HashSet<string>();
            previousOutputFiles = new HashSet<string>();
            Scripts = Site.Scripts;
            filesWritten = new Dictionary<string, FileInfo>();
            totalDuration = new Stopwatch();

            PreProcessors = new PluginCollection<IContentProcessor>(Site);

            Processors = new PluginCollection<ISiteProcessor>(Site);
        }

        private ScriptService Scripts { get; }

        public PluginCollection<IContentProcessor> PreProcessors { get; }

        public PluginCollection<ISiteProcessor> Processors { get; }

        public void Initialize()
        {
            totalDuration.Restart();

            // We collect all previous file entries in the output directory
            CollectPreviousFileEntries();

            filesWritten.Clear();
            Site.Statistics.Reset();

            if (isInitialized)
            {
                return;
            }
            isInitialized = true;

            // If we have any errors, early exit
            if (Site.HasErrors)
            {
                return;
            }
        }

        public void Run()
        {
            Initialize();

            // If we have any errors, early exit
            if (Site.HasErrors)
            {
                return;
            }

            // Start Loading and Preprocessing
            BeginProcess(true);
            try
            {
                LoadAllContent();
            }
            finally
            {
                EndProcess(true);
            }

            // Start to process files
            BeginProcess(false);
            try
            {
                // Process static files
                ProcessPages(Site.StaticFiles, true);

                // Process pages (files with front matter)
                ProcessPages(Site.Pages, true);

                // Process pages (files with front matter)
                ProcessPages(Site.DynamicPages, true);
            }
            finally
            {
                // End the process
                EndProcess(false);
            }

            // Remove output files
            CleanupOutputFiles();

            // Update statistics
            totalDuration.Stop();
            Site.Statistics.TotalTime = totalDuration.Elapsed;
        }

        
        public bool TryCopyFile(FileInfo fromFile, string outputPath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            fromFile = fromFile.Normalize();
            var outputFile = Site.OutputDirectory.CombineToFile(outputPath);
            TrackDestination(outputFile, fromFile);

            if (!outputFile.Exists || (fromFile.LastWriteTime > outputFile.LastWriteTime))
            {
                CreateDirectory(outputFile.Directory);

                if (Site.CanTrace())
                {
                    Site.Trace($"Copy file from [{Site.GetRelativePath(fromFile.FullName, PathFlags.File|PathFlags.Normalize)} to [{outputPath}]");
                }

                fromFile.CopyTo(outputFile.FullName, true);
                return true;
                // Update statistics
                //stat.Static = true;
                //stat.OutputBytes += fromFile.Length;
            }
            return false;
        }

        public bool TryCopyContentToOutput(ContentObject fromFile, string relativePath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            relativePath = PathUtil.NormalizeRelativePath(relativePath, false);
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException($"Path [{relativePath}] cannot be rooted", nameof(relativePath));

            var clock = Stopwatch.StartNew();

            var outputFile = Site.OutputDirectory.CombineToFile(relativePath);
            var outputDir = outputFile.Directory;
            if (outputDir == null)
            {
                throw new ArgumentException("Output directory cannot be empty", nameof(relativePath));
            }
            TrackDestination(outputFile, fromFile.SourceFileInfo);

            var stat = Site.Statistics.GetContentStat(fromFile);

            CreateDirectory(outputDir);

            try
            {
                // If the file has a content, we will use this instead
                if (fromFile.Content != null)
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{Site.GetRelativePath(outputFile.FullName, PathFlags.File | PathFlags.Normalize)}]");
                    }

                    using (var writer = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write)))
                    {
                        writer.Write(fromFile.Content);
                        writer.Flush();

                        // Update statistics
                        stat.Static = false;
                        stat.OutputBytes += writer.BaseStream.Length;
                    }
                }
                // If the source file is not newer than the destination file, don't overwrite it
                else if (fromFile.SourceFileInfo != null && (!outputFile.Exists || (fromFile.ModifiedTime > outputFile.LastWriteTime)))
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{Site.GetRelativePath(outputFile.FullName, PathFlags.File | PathFlags.Normalize)}]");
                    }

                    fromFile.SourceFileInfo.CopyTo(outputFile.FullName, true);

                    // Update statistics
                    stat.Static = true;
                    stat.OutputBytes += fromFile.Length;
                }
            }
            catch (Exception ex)
            {
                Site.Error(fromFile.SourceFileInfo != null
                    ? $"Unable to copy file [{Site.GetRelativePath(fromFile.SourceFileInfo.FullName, PathFlags.File | PathFlags.Normalize)}] to [{outputFile}]. Reason:{ex.GetReason()}"
                    : $"Unable to copy file to [{outputFile}]. Reason:{ex.GetReason()}");
                return false;
            }
            finally
            {
                stat.OutputTime += clock.Elapsed;
            }

            return true;
        }

        public void TrackDestination(FileInfo outputFile, FileInfo sourceFile)
        {
            if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));

            var relativePath = Site.GetRelativePath(outputFile.FullName, PathFlags.File | PathFlags.Normalize);
            var outputFilename = outputFile.FullName;

            if (sourceFile != null)
            {
                FileInfo previousSourceFile;
                if (filesWritten.TryGetValue(outputFilename, out previousSourceFile))
                {
                    Site.Error(
                        $"The content [{Site.GetRelativePath(previousSourceFile.FullName, PathFlags.File | PathFlags.Normalize)}] and [{Site.GetRelativePath(sourceFile.FullName, PathFlags.File | PathFlags.Normalize)}] have the same Url output [{relativePath}]");
                }
                else
                {
                    filesWritten.Add(outputFilename, sourceFile);
                }
            }

            // If the directory is used for a new file, remove it from the list of previous directories
            // Note that we remove even if things are not working after, so that previous files are kept 
            // in case of an error
            var previousDir = outputFile.Directory;
            while (previousDir != null)
            {
                previousOutputDirectories.Remove(previousDir.FullName);
                previousDir = previousDir.Parent;
            }
            previousOutputFiles.Remove(outputFilename);
        }

        private void BeginProcess(bool preProcess)
        {
            var statistics = Site.Statistics;

            // Callback plugins once files have been initialized but not yet processed
            var processors = preProcess ? PreProcessors.Cast<ISiteProcessor>() : Processors;
            int i = 0;
            var clock = Stopwatch.StartNew();
            foreach (var processor in processors)
            {
                var stat = statistics.GetPluginStat(processor);
                stat.Order = i;

                clock.Restart();
                processor.BeginProcess();
                clock.Stop();
                stat.BeginProcessTime = clock.Elapsed;
                i++;
            }
        }

        private void EndProcess(bool preProcess)
        {
            var statistics = Site.Statistics;

            // Callback plugins once files have been initialized but not yet processed
            var processors = preProcess ? PreProcessors.Cast<ISiteProcessor>() : Processors;
            var clock = Stopwatch.StartNew();
            foreach (var processor in processors)
            {
                clock.Restart();

                processor.EndProcess();

                // Update statistics
                clock.Stop();
                statistics.GetPluginStat(processor).EndProcessTime += clock.Elapsed;
            }
        }

        public bool TryPreparePage(ContentObject page)
        {
            if (Scripts.TryRunFrontMatter(page.Script, page))
            {
                if (page.Script != null && TryEvaluate(page))
                {
                    // If page is discarded, skip it
                    if (page.Discard)
                    {
                        return false;
                    }

                    var pendingPageProcessors = new OrderedList<IContentProcessor>();
                    return TryProcessPage(page, PreProcessors, pendingPageProcessors, false);
                }
            }
            return false;
        }

        public void ProcessPages(PageCollection pages, bool copyOutput)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));

            // Process pages
            var pageProcessors = new OrderedList<IContentProcessor>(Processors.OfType<IContentProcessor>());
            var pendingPageProcessors = new OrderedList<IContentProcessor>();
            foreach (var page in pages)
            {
                TryProcessPage(page, pageProcessors, pendingPageProcessors, copyOutput);
            }
        }

        private void LoadAllContent()
        {
            Site.StaticFiles.Clear();
            Site.Pages.Clear();

            // Load a content asynchronously
            var contentLoaderBlock = new TransformBlock<ContentReference, ContentObject>(
                async reference => await LoadContent(reference),
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = false,
                    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
                });

            // Add the content loaded to the correct list (static vs pages)
            var contentAdderBlock = new ActionBlock<ContentObject>(content =>
            {
                // We don't need lock as this block has a MaxDegreeOfParallelism = 1
                var list = content.ScriptObjectLocal != null ? Site.Pages : Site.StaticFiles;
                list.Add(content);
            });

            // Link loader and adder
            contentLoaderBlock.LinkTo(contentAdderBlock, new DataflowLinkOptions() { PropagateCompletion = true });

            // We don't use a block for this part (annoying without struct tuples)

            // Get the list of root directories from themes
            var rootDirectories = new List<FolderInfo>(Site.ContentDirectories);

            // Compute the list of files that we will actually process
            var filesLoaded = new HashSet<string>();
            foreach (var rootDirectory in rootDirectories)
            {
                var directories = new Queue<FolderInfo>();
                directories.Enqueue(rootDirectory);
                while (directories.Count > 0)
                {
                    var nextDirectory = directories.Dequeue();
                    foreach (var contentReference in LoadDirectory(rootDirectory, nextDirectory, directories, filesLoaded))
                    {
                        contentLoaderBlock.Post(contentReference);
                    }
                }
            }

            // We are done loading content, wait for completion
            contentLoaderBlock.Complete();
            contentAdderBlock.Completion.Wait();

            // Finally, we sort pages by natural order
            Site.Pages.Sort();
        }

        private IEnumerable<ContentReference> LoadDirectory(FolderInfo rootDirectory, DirectoryInfo directory, Queue<FolderInfo> directoryQueue, HashSet<string> loaded)
        {
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

                    yield return new ContentReference(rootDirectory, (FileInfo)entry);
                }
                else if (!entry.Name.StartsWith("_") && entry.Name != SiteObject.PrivateDirectoryName)
                {
                    directoryQueue.Enqueue((FolderInfo)entry);
                }
            }
        }

        private async Task<ContentObject> LoadContent(ContentReference arg)
        {
            ContentObject page = null;
            var buffer = new byte[16];

            var file = arg.FileInfo;
            var rootDirectory = arg.RootFolder;

            var clock = Stopwatch.StartNew();
            var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var count = await stream.ReadAsync(buffer, 0, buffer.Length);
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
                    page = await LoadPageScript(Site, stream, rootDirectory, file);
                    stream = null;
                }
                else
                {
                    page = new ContentObject(Site, rootDirectory, file);
                }
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
        private static async Task<ContentObject> LoadPageScript(SiteObject site, Stream stream, DirectoryInfo rootDirectory, FileInfo file)
        {
            // Read the stream
            var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            // Early dispose the stream
            stream.Dispose();

            ContentObject page = null;

            // Parse the page, using front-matter mode
            var scriptPage = site.Scripts.ParseScript(content, file.FullName, ScriptMode.FrontMatter);
            if (!scriptPage.HasErrors)
            {
                page = new ContentObject(site, rootDirectory, file)
                {
                    Script = scriptPage.Page
                };

                var evalClock = Stopwatch.StartNew();
                if (site.Builder.TryPreparePage(page))
                {
                    evalClock.Stop();

                    // Update statistics
                    var contentStat = site.Statistics.GetContentStat(page);

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

        private bool TryProcessPage(ContentObject page, OrderedList<IContentProcessor> pageProcessors, OrderedList<IContentProcessor> pendingPageProcessors, bool copyOutput)
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
            // (more priority) to the begining of the list (less priority).
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
                    var result = processor.TryProcess(page);
                    clock.Stop();

                    if (result != ContentResult.None)
                    {
                        // Update statistics per plugin
                        var statistics = Site.Statistics;
                        var stat = statistics.GetPluginStat(processor);
                        stat.PageCount++;
                        stat.ProcessTime += clock.Elapsed;

                        hasBeenProcessed = true;
                        pendingPageProcessors.RemoveAt(i);
                        breakProcessing = result == ContentResult.Break;
                        break;
                    }
                }
            }
            pendingPageProcessors.Clear();

            // Copy only if the file are marked as include
            if (copyOutput && !breakProcessing && !page.Discard)
            {
                Site.Builder.TryCopyContentToOutput(page, page.GetDestinationPath());
            }

            return true;
        }

        private bool TryEvaluate(ContentObject page)
        {
            if (page.ScriptObjectLocal == null)
            {
                page.ScriptObjectLocal = new ScriptObject();
            }

            var clock = Stopwatch.StartNew();
            try
            {
                return Site.Scripts.TryEvaluate(page, page.Script, page.SourceFile, page.ScriptObjectLocal);
            }
            finally
            {
                clock.Stop();
                Site.Statistics.GetContentStat(page).EvaluateTime += clock.Elapsed;
            }
        }

        public void CreateDirectory(DirectoryInfo directory)
        {
            if (!directory.Exists)
            {
                if (Site.CanTrace())
                {
                    Site.Trace($"Create directory [{Site.GetRelativePath(directory.FullName, PathFlags.Directory | PathFlags.Normalize)}]");
                }

                try
                {
                    directory.Create();
                }
                catch (Exception ex)
                {
                    Site.Error($"Unable to create directory [{Site.GetRelativePath(directory.FullName, PathFlags.Directory | PathFlags.Normalize)}]. Reason:{ex.GetReason()}");
                }
            }
        }

        private void CleanupOutputFiles()
        {
            // Remove all previous files that have not been generated
            foreach (var outputFilename in previousOutputFiles)
            {
                var outputFile = new FileInfo(outputFilename);
                try
                {
                    if (outputFile.Exists)
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete file [{Site.GetRelativePath(outputFile.FullName, PathFlags.File | PathFlags.Normalize)}]");
                        }
                        outputFile.Delete();
                    }
                }
                catch (Exception)
                {
                }
            }

            foreach (var outputDirectory in previousOutputDirectories)
            {
                try
                {
                    if (Directory.Exists(outputDirectory))
                    {
                        if (Site.CanTrace())
                        {
                            Site.Trace($"Delete directory [{Site.GetRelativePath(outputDirectory, PathFlags.Directory | PathFlags.Normalize)}]");
                        }
                        Directory.Delete(outputDirectory);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void CollectPreviousFileEntries()
        {
            var outputDirectoryInfo = new DirectoryInfo(Site.OutputDirectory);
            previousOutputDirectories.Clear();
            previousOutputFiles.Clear();

            if (!outputDirectoryInfo.Exists)
            {
                return;
            }

            var directories = new Queue<DirectoryInfo>();
            directories.Enqueue(outputDirectoryInfo);

            while (directories.Count > 0)
            {
                var nextDirectory = directories.Dequeue();

                if (nextDirectory != outputDirectoryInfo)
                {
                    previousOutputDirectories.Add(nextDirectory.FullName);
                }

                foreach (var entry in nextDirectory.EnumerateFileSystemInfos())
                {
                    if (entry is FileInfo)
                    {
                        previousOutputFiles.Add(((FileInfo)entry).FullName);
                    }
                    else if (entry is DirectoryInfo)
                    {
                        directories.Enqueue((DirectoryInfo)entry);
                    }
                }
            }
        }

        internal struct ContentReference
        {
            public ContentReference(FolderInfo rootFolder, FileInfo fileInfo)
            {
                RootFolder = rootFolder;
                FileInfo = fileInfo;
            }

            public readonly FolderInfo RootFolder;

            public readonly FileInfo FileInfo;
        }
    }
}