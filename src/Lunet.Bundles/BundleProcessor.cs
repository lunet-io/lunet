// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lunet.Core;
using Lunet.Helpers;
using Zio;

namespace Lunet.Bundles;

public class BundleProcessor : ProcessorBase<BundlePlugin>
{
    public BundleProcessor(BundlePlugin bundlePlugin) : base(bundlePlugin)
    {
        Minifiers = new OrderedList<IContentMinifier>();
    }

    public OrderedList<IContentMinifier> Minifiers { get; }

    public override void Process(ProcessingStage stage)
    {
        Debug.Assert(stage == ProcessingStage.BeforeProcessingContent);

        // If we don't have any bundles, early exit
        if (Plugin.List.Count == 0)
        {
            return;
        }

        // Collect bundles that are used by pages
        var bundleUsed = new HashSet<BundleObject>();
        foreach (var page in Site.Pages)
        {
            // Get the bundle setup for the page, or use the default otherwise
            var bundleName = page.GetSafeValue<string>("bundle");
            var bundle = Plugin.GetOrCreateBundle(bundleName);
            bundleUsed.Add(bundle);
        }

        // Compute a cache of current static files
        var staticFiles = new Dictionary<UPath, ContentObject>();
        foreach (var staticFile in Site.StaticFiles)
        {
            staticFiles.Add(staticFile.SourceFile.Path, staticFile);
        }

        // Process bundle
        foreach (var bundle in bundleUsed)
        {
            ProcessBundle(bundle, staticFiles);
        }
    }

    private void ProcessBundle(BundleObject bundle, Dictionary<UPath, ContentObject> staticFiles)
    {
        // Sanitize url destination for the bundle
        foreach (var urlDestType in bundle.UrlDestination.Keys.ToList())
        {
            var urlDest = bundle.UrlDestination.GetSafeValue<string>(urlDestType);
            if (string.IsNullOrWhiteSpace(urlDest))
            {
                Site.Warning(
                    $"Invalid null or empty url_dest for type [{urlDestType}] in bundle [{bundle.Name}]. Reset default to \"/res/\"");
                urlDest = "/res/";
            }

            Uri? url;
            if (!Uri.TryCreate(urlDest, UriKind.Relative, out url))
            {
                Site.Error(
                    $"Unable to parse url_dest [{urlDest}] for type [{urlDestType}] in bundle [{bundle.Name}]");
            }

            var finalUrl = PathUtil.NormalizeUrl(urlDest, true);

            if (finalUrl != urlDest)
            {
                bundle.UrlDestination[urlDestType] = finalUrl;
            }
        }

        ProcessBundleLinks(bundle, staticFiles);
    }

    private void ProcessBundleLinks(BundleObject bundle, Dictionary<UPath, ContentObject> staticFiles)
    {
        Dictionary<string, ConcatGroup>? concatBuilders = null;
        if (bundle.Concat)
        {
            concatBuilders = new Dictionary<string, ConcatGroup>();
            foreach (var type in bundle.UrlDestination)
            {
                if (!concatBuilders.ContainsKey(type.Key))
                {
                    concatBuilders[type.Key] = new ConcatGroup(type.Key);
                }
            }
        }

        // Expand wildcard * links
        for (int i = 0; i < bundle.Links.Count; i++)
        {
            var link = bundle.Links[i];
            var path = link.Path;
            var url = link.Url;
            var urlWithoutBasePath = link.UrlWithoutBasePath;
            if (string.IsNullOrEmpty(path) || !path.Contains("*")) continue;

            // Always remove the link
            bundle.Links.RemoveAt(i);

            var upath = (UPath) path;

            bool matchingInFileSystem = false;
            foreach (var file in Site.FileSystem.EnumerateFileSystemEntries(upath.GetDirectory(), upath.GetName()))
            {
                var newLink = new BundleLink(bundle, link.Type, (string)file.Path, url + file.Path.GetName(), urlWithoutBasePath + file.Path.GetName(), link.Mode);
                bundle.Links.Insert(i++, newLink);
                matchingInFileSystem = true;
            }

            if (!matchingInFileSystem)
            {
                foreach (var file in Site.MetaFileSystem.EnumerateFileSystemEntries(upath.GetDirectory(), upath.GetName()))
                {
                    var newLink = new BundleLink(bundle, link.Type, (string)file.Path, url + file.Path.GetName(), urlWithoutBasePath + file.Path.GetName(), link.Mode);
                    bundle.Links.Insert(i++, newLink);
                }
            }

            // Cancel the double i++
            i--;
        }
            
        // Collect minifier
        IContentMinifier? minifier = null;
        if (bundle.Minify)
        {
            var minifierName = bundle.Minifier;
            foreach (var min in Minifiers)
            {
                if (minifierName == null || min.Name == minifierName)
                {
                    minifier = min;
                    break;
                }
            }

            if (minifier == null)
            {
                Site.Warning($"Minify is setup for bundle [{bundle.Name}] but no minifiers are registered (Minified requested: {minifierName ?? "default"})");
            }
        }

        bool bundleAsIs = !bundle.Minify && !bundle.Concat;
        
        // Process links
        for (int i = 0; i < bundle.Links.Count; i++)
        {
            var link = bundle.Links[i];
            var path = link.Path;
            var url = link.Url;
            var urlWithoutPath = link.UrlWithoutBasePath;
            if (url != null)
            {
                if (!UPath.TryParse(url, out _))
                {
                    Site.Error($"Invalid absolute url [{url}] in bundle [{bundle.Name}]");
                }
            }
                
            if (path != null)
            {
                path = ((UPath)path).FullName;
                link.Path = path;

                var isExistingContent = staticFiles.TryGetValue(path, out var currentContent);

                if (url == null)
                {
                    var outputUrlDirectory = bundle.UrlDestination[link.Type];
                    // If the file is private or meta, we need to copy to the output
                    // bool isFilePrivateOrMeta = Site.IsFilePrivateOrMeta(entry.FullName);
                    urlWithoutPath = outputUrlDirectory + Path.GetFileName(path);
                    url = new UPath($"{Site.BasePath}/.{urlWithoutPath}").FullName;
                    link.Url = url;
                    link.UrlWithoutBasePath = urlWithoutPath;
                }

                FileSystemItem entry = default;

                // Process file by existing processors
                if (currentContent == null)
                {
                    // Check first site, then meta
                    var fs = Site.FileSystem;
                    entry = new FileSystemItem(fs, path, false);
                    bool exists = entry.Exists();
                    if (!exists)
                    {
                        fs = Site.MetaFileSystem;
                        entry = new FileSystemItem(Site.MetaFileSystem, path, false);
                        exists = entry.Exists();
                    }
                    
                    if (exists)
                    {
                        // Fetch the datetime of the content if we don't process it for concat/minify
                        if (bundleAsIs)
                        {
                            entry.CreationTime = fs.GetCreationTime(path);
                            entry.LastAccessTime = fs.GetLastAccessTime(path);
                            entry.LastWriteTime = fs.GetLastWriteTime(path);
                        }

                        currentContent = new FileContentObject(Site, entry);
                    }
                    else
                    {
                        Site.Error($"Unable to find content [{path}] in bundle [{bundle.Name}]");
                    }
                }

                if (currentContent != null)
                {
                    currentContent.Url = url;
                    currentContent.UrlWithoutBasePath = urlWithoutPath;
                        
                    var listTemp = new PageCollection() { currentContent };
                    Site.Content.ProcessPages(listTemp, false);
                    link.ContentObject = currentContent;

                    bool isRawContent = link.Type == BundleObjectProperties.ContentType;
                        
                    // If we require concat and/or minify, we preload the content of the file
                    if (!isRawContent && (bundle.Concat || bundle.Minify))
                    {
                        try
                        {
                            link.Content = currentContent.Content ?? (entry.IsEmpty ? string.Empty : entry.ReadAllText());

                            // Minify content separately
                            if (bundle.Minify && minifier != null)
                            {
                                Minify(minifier, link, bundle.MinifyExtension);
                            }
                        }
                        catch (Exception ex)
                        {
                            Site.Error(ex, $"Unable to load content [{path}] while trying to concatenate for bundle [{bundle.Name}]. Reason: {ex.GetReason()}");
                        }
                    }

                    // Remove sourcemaps (TODO: make this configurable)
                    RemoveSourceMaps(link);

                    // If we are concatenating
                    if (!isRawContent && concatBuilders != null)
                    {
                        currentContent.Discard = true;

                        // Remove this link from the list of links, as we are going to squash them after
                        bundle.Links.RemoveAt(i);
                        i--;

                        concatBuilders[link.Type].Pages.Add(currentContent);
                        concatBuilders[link.Type].AppendContent(link.Mode, link.Content ?? string.Empty);
                    }
                    else if (!isExistingContent)
                    {
                        Site.StaticFiles.Add(currentContent);
                        staticFiles.Add(path, currentContent);
                    }
                }
            }
        }

        // Concatenate files if necessary
        if (concatBuilders != null)
        {
            foreach (var builderGroup in concatBuilders)
            {
                foreach(var contentPerMode in builderGroup.Value.ModeToBuilders)
                {
                    var mode = contentPerMode.Key;
                    var builder = contentPerMode.Value;
                    if (builder.Length > 0)
                    {
                        var type = builderGroup.Key;
                        var outputUrlDirectory = bundle.UrlDestination[type]?.ToString() ?? string.Empty;

                        // If the file is private or meta, we need to copy to the output
                        // bool isFilePrivateOrMeta = Site.IsFilePrivateOrMeta(entry.FullName);
                        var url = outputUrlDirectory + bundle.Name + (string.IsNullOrEmpty(mode) ? $".{type}" : $"-{mode}.{type}");
                        var newStaticFile = new DynamicContentObject(Site, url)
                        {
                            Content = builder.ToString()
                        };
                        Site.DynamicPages.Add(newStaticFile);

                        // Add file dependencies
                        foreach (var page in builderGroup.Value.Pages)
                        {
                            newStaticFile.Dependencies.Add(new PageContentDependency(page));
                        }

                        var link = new BundleLink(bundle, type, null, url, newStaticFile.UrlWithoutBasePath, mode)
                        {
                            Content = newStaticFile.Content,
                            ContentObject = newStaticFile
                        };

                        bundle.Links.Add(link);
                    }
                }
            }
        }

        foreach (var link in bundle.Links)
        {
            var contentObject = link.ContentObject;
            if (contentObject != null)
            {
                link.Url = contentObject.Url;
            }
        }
    }


    private static readonly Regex RegexSourceMapSimple = new Regex(@"^//#\s*sourceMappingURL=(.|\r|\n)*", RegexOptions.Multiline);
    private static readonly Regex RegexSourceMapMulti = new Regex(@"^/\*#\s*sourceMappingURL=(.|\r|\n)*\*/", RegexOptions.Multiline);

    private static void RemoveSourceMaps(BundleLink link)
    {
        var content = link.Content;
        if (content == null) return;
        content = RegexSourceMapSimple.Replace(content, string.Empty);
        content = RegexSourceMapMulti.Replace(content, string.Empty);
        link.Content = content;
        if (link.ContentObject != null)
        {
            link.ContentObject.Content = content;
        }
    }

    private static void Minify(IContentMinifier minifier, BundleLink link, string? minifyExtension)
    {
        var contentObject = link.ContentObject;
        // Don't try to minify content that is already minified
        if (contentObject != null && link.Content != null && !string.IsNullOrEmpty(link.Path) && !link.Path.EndsWith($".min.{link.Type}"))
        {
            var minifiedResult = minifier.Minify(link.Type, link.Content, link.Path);
            contentObject.Content = minifiedResult;
            link.Content = minifiedResult;

            var minExtension = (minifyExtension ?? string.Empty) + "." + link.Type;
            if (!string.IsNullOrEmpty(contentObject.Url) && !contentObject.Url.EndsWith(minExtension))
            {
                var url = Path.ChangeExtension(contentObject.Url, minExtension);
                contentObject.Url = url;
            }
        }
    }

    /// <summary>
    /// A class that concatenates strings per kind (css, js...)
    ///
    /// TODO: Concatenation should be a pluggable features per content kind
    /// </summary>
    private class ConcatGroup
    {
        public ConcatGroup(string kind)
        {
            Kind = kind;
            Pages = new List<ContentObject>();
            ModeToBuilders = new Dictionary<string, StringBuilder> {{string.Empty, new StringBuilder()}};
        }

        public string Kind { get; }

        public List<ContentObject> Pages { get; }

        public Dictionary<string, StringBuilder> ModeToBuilders { get; }

        // used to store if the css kind has already a @charset rules at the top
        private bool _hasCssCharset;

        public void AppendContent(string? mode, string content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            mode ??= string.Empty;
            if (!ModeToBuilders.TryGetValue(mode, out var builder))
            {
                builder = new StringBuilder();
                ModeToBuilders.Add(mode, builder);
            }

            // The @charset rules in css cannot be concatened but must come first in the css
            // so we are handling this case by setting charset rule at the beginning of the concat
            if (Kind == "css" && content.StartsWith("@charset "))
            {
                var endOfCharset = content.IndexOf(';');
                string? charset = null;
                if (endOfCharset > 0)
                {
                    charset = content.Substring(0, endOfCharset + 1);
                    content = content.Substring(endOfCharset + 1);
                }
                else
                {
                    charset = content;
                    content = "";
                }

                if (!_hasCssCharset)
                {
                    builder.Insert(0, charset);
                    _hasCssCharset = true;
                }
                else
                {
                    // TODO: in case of having multiple charset we should verify that
                    // they are the same otherwise we could not concat the files!
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                builder.Append(content);
                builder.Append('\n');
            }
        }
    }
}
