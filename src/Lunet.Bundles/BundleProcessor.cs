// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lunet.Core;
using Lunet.Helpers;
using Zio;

namespace Lunet.Bundles
{
    public class BundleProcessor : ProcessorBase<BundlePlugin>
    {
        public BundleProcessor(BundlePlugin bundlePlugin) : base(bundlePlugin)
        {
            Minifiers = new OrderedList<IContentMinifier>();
        }

        public OrderedList<IContentMinifier> Minifiers { get; }

        public override void Process()
        {
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

                Uri url;
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
            Dictionary<string, ConcatGroup> concatBuilders = null;
            if (bundle.Concat)
            {
                concatBuilders = new Dictionary<string, ConcatGroup>();
                foreach (var type in bundle.UrlDestination)
                {
                    if (!concatBuilders.ContainsKey(type.Key))
                    {
                        concatBuilders[type.Key] = new ConcatGroup();
                    }
                }
            }

            // Process links
            for (int i = 0; i < bundle.Links.Count; i++)
            {
                var link = bundle.Links[i];
                var path = link.Path;
                var url = link.Url;
                if (url != null)
                {
                    Uri result;
                    if (!Uri.TryCreate(url, UriKind.Absolute, out result))
                    {
                        Site.Error($"Invalid absolute url [{url}] in bundle [{bundle.Name}]");
                    }
                }
                else if (path != null)
                {
                    path = ((UPath)path).FullName;
                    link.Path = path;
                    var entry = new FileEntry(Site.FileSystem, (UPath)path);

                    var outputUrlDirectory = bundle.UrlDestination[link.Type];

                    ContentObject currentContent;
                    bool isExistingContent = false;
                    if (staticFiles.TryGetValue(entry.FullName, out currentContent))
                    {
                        isExistingContent = true;
                        currentContent.Url = outputUrlDirectory + Path.GetFileName(currentContent.Url);
                    }
                    // If the file is private or meta, we need to copy to the output
                    // bool isFilePrivateOrMeta = Site.IsFilePrivateOrMeta(entry.FullName);
                    url = outputUrlDirectory + Path.GetFileName(path);
                    link.Url = url;

                    // Process file by existing processors
                    if (currentContent == null)
                    {
                        if (entry.Exists)
                        {
                            currentContent = new ContentObject(Site, new FileEntry(Site.FileSystem, path)) { Url = url };
                        }
                        else
                        {
                            Site.Error($"Unable to find content [{path}] in bundle [{bundle.Name}]");
                        }
                    }

                    if (currentContent != null)
                    {
                        var listTemp = new PageCollection() { currentContent };
                        Site.Content.ProcessPages(listTemp, false);
                        link.ContentObject = currentContent;

                        // If we require concat and/or minify, we preload the content of the file
                        if (bundle.Concat || bundle.Minify)
                        {
                            try
                            {
                                link.Content = currentContent.Content ?? File.ReadAllText(entry.FullName);
                            }
                            catch (Exception ex)
                            {
                                Site.Error(
                                    $"Unable to load content [{path}] while trying to concatenate for bundle [{bundle.Name}]. Reason: {ex.GetReason()}");
                            }
                        }

                        // If we are concatenating
                        if (concatBuilders != null)
                        {
                            currentContent.Discard = true;

                            // Remove this link from the list of links, as we are going to squash them after
                            bundle.Links.RemoveAt(i);
                            i--;

                            concatBuilders[link.Type].Pages.Add(currentContent);
                            concatBuilders[link.Type].Builder.AppendLine(link.Content);
                        }
                        else if (!isExistingContent)
                        {
                            Site.StaticFiles.Add(currentContent);
                            staticFiles.Add(entry.FullName, currentContent);
                        }
                    }
                }
            }

            // Concatenate files if necessary
            if (concatBuilders != null)
            {
                foreach (var builderGroup in concatBuilders)
                {
                    var builder = builderGroup.Value.Builder;
                    if (builder.Length > 0)
                    {
                        var type = builderGroup.Key;
                        var outputUrlDirectory = bundle.UrlDestination[type];

                        // If the file is private or meta, we need to copy to the output
                        // bool isFilePrivateOrMeta = Site.IsFilePrivateOrMeta(entry.FullName);
                        var url = outputUrlDirectory + bundle.Name + "." + type;
                        var newStaticFile = new ContentObject(Site)
                        {
                            Url = url,
                            Content = builder.ToString()
                        };
                        Site.DynamicPages.Add(newStaticFile);

                        // Add file dependencies
                        foreach (var page in builderGroup.Value.Pages)
                        {
                            newStaticFile.Dependencies.Add(new PageContentDependency(page));
                        }

                        var link = new BundleLink(bundle, type, null, url)
                        {
                            Content = newStaticFile.Content,
                            ContentObject = newStaticFile
                        };

                        bundle.Links.Add(link);
                    }
                }
            }

            // Minify entries
            if (bundle.Minify)
            {
                var minifierName = bundle.Minifier;
                IContentMinifier minifier = null;
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
                else
                {
                    foreach (var link in bundle.Links)
                    {
                        var contentObject = link.ContentObject;
                        if (contentObject != null)
                        {
                            contentObject.Content = minifier.Minify(link.Type, link.Content);

                            var minExtension = (bundle.MinifyExtension ?? string.Empty) + "." + link.Type;
                            if (!contentObject.Url.EndsWith(minExtension))
                            {
                                var url = Path.ChangeExtension(contentObject.Url, minExtension);
                                contentObject.Url = url;
                            }
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

        private class ConcatGroup
        {
            public ConcatGroup()
            {
                Pages = new List<ContentObject>();
                Builder = new StringBuilder();
            }
            public List<ContentObject> Pages { get; }

            public StringBuilder Builder { get; }
        }
    }
}
