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
using Lunet.Scripts;
using Scriban.Parsing;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    public interface IFrontMatterParser
    {
        bool CanHandle(byte[] header, int index);

        bool TryParse(string text, ContentObject content, out TextPosition position);
    }

    public class ContentPlugin : SitePlugin
    {
        private readonly HashSet<DirectoryEntry> previousOutputDirectories;
        private readonly HashSet<FileEntry> previousOutputFiles;
        private readonly Dictionary<FileEntry, FileEntry> filesWritten;
        private bool isInitialized;
        private readonly Stopwatch totalDuration;

        public ContentPlugin(SiteObject site) : base(site)
        {
            previousOutputDirectories = new HashSet<DirectoryEntry>();
            previousOutputFiles = new HashSet<FileEntry>();
            Scripts = Site.Scripts;
            filesWritten = new Dictionary<FileEntry, FileEntry>();
            totalDuration = new Stopwatch();

            BeforeLoadingProcessors = new OrderedList<ISiteProcessor>();

            ContentProcessors = new OrderedList<IContentProcessor>();

            AfterContentProcessors = new OrderedList<ISiteProcessor>();

            FrontMatterParsers = new OrderedList<IFrontMatterParser>();
        }

        private ScriptingPlugin Scripts { get; }

        public OrderedList<ISiteProcessor> BeforeLoadingProcessors { get; }

        public OrderedList<IContentProcessor> ContentProcessors { get; }

        public OrderedList<ISiteProcessor> AfterContentProcessors { get; }

        public OrderedList<IFrontMatterParser> FrontMatterParsers { get; }

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
            RunProcess(BeforeLoadingProcessors);
            try
            {
                LoadAllContent();
            }
            finally
            {
                RunProcess(AfterContentProcessors);
            }

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
                // TODO: Add
            }

            // Remove output files
            CleanupOutputFiles();

            // Update statistics
            totalDuration.Stop();
            Site.Statistics.TotalTime = totalDuration.Elapsed;
        }

        
        public bool TryCopyFile(FileEntry fromFile, UPath outputPath)
        {
            if (fromFile == null) throw new ArgumentNullException(nameof(fromFile));
            outputPath.AssertAbsolute(nameof(outputPath));

            var outputFile = new FileEntry(Site.OutputFileSystem, outputPath);
            TrackDestination(outputFile, fromFile);

            if (!outputFile.Exists || (fromFile.LastWriteTime > outputFile.LastWriteTime))
            {
                CreateDirectory(outputFile.Directory);

                if (Site.CanTrace())
                {
                    Site.Trace($"Copy file from [{fromFile} to [{outputPath}]");
                }

                fromFile.CopyTo(outputFile.FullName, true);
                return true;
                // Update statistics
                //stat.Static = true;
                //stat.OutputBytes += fromFile.Length;
            }
            return false;
        }

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
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{outputFile}]");
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
                else if (fromFile.SourceFile != null && (!outputFile.Exists || (fromFile.ModifiedTime > outputFile.LastWriteTime)))
                {
                    if (Site.CanTrace())
                    {
                        Site.Trace($"Write file [{outputFile}]");
                    }

                    fromFile.SourceFile.CopyTo(outputFile.FullName, true);

                    // Update statistics
                    stat.Static = true;
                    stat.OutputBytes += fromFile.Length;
                }
            }
            catch (Exception ex)
            {
                Site.Error(fromFile.SourceFile != null
                    ? $"Unable to copy file [{fromFile}] to [{outputFile}]. Reason:{ex.GetReason()}"
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

            if (sourceFile != null)
            {
                FileEntry previousSourceFile;
                if (filesWritten.TryGetValue(outputFile, out previousSourceFile))
                {
                    Site.Error($"The content [{previousSourceFile}] and [{sourceFile}] have the same Url output [{sourceFile}]");
                }
                else
                {
                    filesWritten.Add(outputFile, sourceFile);
                }
            }

            // If the directory is used for a new file, remove it from the list of previous directories
            // Note that we remove even if things are not working after, so that previous files are kept 
            // in case of an error
            var previousDir = outputFile.Directory;
            while (previousDir != null)
            {
                previousOutputDirectories.Remove(previousDir);
                previousDir = previousDir.Parent;
            }
            previousOutputFiles.Remove(outputFile);
        }

        private void RunProcess(IEnumerable<ISiteProcessor> processors)
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
                processor.Process();
                clock.Stop();
                stat.BeginProcessTime = clock.Elapsed;
                i++;
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
                    return TryProcessPage(page, BeforeLoadingProcessors.OfType<IContentProcessor>(), pendingPageProcessors, false);
                }
            }
            return false;
        }

        public void ProcessPages(PageCollection pages, bool copyOutput)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));

            // Process pages
            var pendingPageProcessors = new OrderedList<IContentProcessor>();
            foreach (var page in pages)
            {
                TryProcessPage(page, ContentProcessors, pendingPageProcessors, copyOutput);
            }
        }

        private void LoadAllContent()
        {
            Site.StaticFiles.Clear();
            Site.Pages.Clear();

            // Load a content asynchronously
            var contentLoaderBlock = new TransformBlock<FileEntry, ContentObject>(
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

            // Compute the list of files that we will actually process
            var directories = new Queue<DirectoryEntry>();
            directories.Enqueue(new DirectoryEntry(Site.FileSystem, UPath.Root));
            while (directories.Count > 0)
            {
                var nextDirectory = directories.Dequeue();
                foreach (var contentReference in LoadDirectory(nextDirectory, directories))
                {
                    contentLoaderBlock.Post(contentReference);
                }
            }

            // We are done loading content, wait for completion
            contentLoaderBlock.Complete();
            contentAdderBlock.Completion.Wait();

            // Finally, we sort pages by natural order
            Site.Pages.Sort();
        }

        private IEnumerable<FileEntry> LoadDirectory(DirectoryEntry directory, Queue<DirectoryEntry> directoryQueue)
        {
            foreach (var entry in directory.EnumerateEntries())
            {
                if (entry.Name == SiteObject.DefaultConfigFileName)
                {
                    continue;
                }

                if (entry is FileEntry)
                {
                    yield return (FileEntry) entry;
                }
                else if (!entry.Name.StartsWith("_") && entry.Name != SiteObject.TempFolderName)
                {
                    directoryQueue.Enqueue(entry.Parent);
                }
            }
        }

        private async Task<ContentObject> LoadContent(FileEntry file)
        {
            ContentObject page = null;
            var buffer = new byte[16];

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

                if (buffer[startFrontMatter] == '+' && buffer[startFrontMatter + 1] == '+' && buffer[startFrontMatter + 2] == '+')
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
                }

                if (hasFrontMatter)
                {
                    page = await LoadPageScript(Site, stream, file);
                    stream = null;
                }
                else
                {
                    page = new ContentObject(Site, file);
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
        private static async Task<ContentObject> LoadPageScript(SiteObject site, Stream stream, FileEntry file)
        {
            // Read the stream
            var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            // Early dispose the stream
            stream.Dispose();

            ContentObject page = null;

            // Parse the page, using front-matter mode
            var scriptPage = site.Scripts.ParseScript(content, file.FullName, ScriptMode.FrontMatterAndContent);
            if (!scriptPage.HasErrors)
            {
                page = new ContentObject(site, file)
                {
                    Script = scriptPage.Page
                };

                var evalClock = Stopwatch.StartNew();
                if (site.Content.TryPreparePage(page))
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

        private bool TryProcessPage(ContentObject page, IEnumerable<IContentProcessor> pageProcessors, OrderedList<IContentProcessor> pendingPageProcessors, bool copyOutput)
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
                        stat.ContentProcessTime += clock.Elapsed;

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
                Site.Content.TryCopyContentToOutput(page, page.GetDestinationPath());
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
                return Site.Scripts.TryEvaluate(page, page.Script, page.SourceFile.Path, page.ScriptObjectLocal);
            }
            finally
            {
                clock.Stop();
                Site.Statistics.GetContentStat(page).EvaluateTime += clock.Elapsed;
            }
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
                    Site.Error($"Unable to create directory [{directory}]. Reason:{ex.GetReason()}");
                }
            }
        }

        private void CleanupOutputFiles()
        {
            // Remove all previous files that have not been generated
            foreach (var outputFile in previousOutputFiles)
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

            foreach (var outputDirectory in previousOutputDirectories)
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
            previousOutputDirectories.Clear();
            previousOutputFiles.Clear();

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
                    previousOutputDirectories.Add(nextDirectory);
                }

                foreach (var entry in nextDirectory.EnumerateEntries())
                {
                    if (entry is FileEntry)
                    {
                        previousOutputFiles.Add(((FileEntry)entry));
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