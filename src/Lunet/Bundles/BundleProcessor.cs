// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Plugins;
using Scriban.Runtime;

namespace Lunet.Bundles
{
    public class BundleProcessor : ProcessorBase
    {
        public override string Name => "bundler";


        public override void BeginProcess()
        {
            // If we don't have any bundles, early exit
            if (Site.Bundles.List.Count == 0)
            {
                return;
            }

            // Collect bundles that are used by pages
            var bundleUsed = new HashSet<BundleObject>();
            foreach (var page in Site.Pages)
            {
                // Get the bundle setup for the page, or use the default otherwise
                var bundleName = page.DynamicObject.GetSafeValue<string>("bundle");
                var bundle = Site.Bundles.GetOrCreateBundle(bundleName);
                bundleUsed.Add(bundle);
            }


            var staticFiles = new Dictionary<FileInfo, ContentObject>();
            foreach (var staticFile in Site.StaticFiles)
            {
                staticFiles.Add(staticFile.SourceFileInfo, staticFile);
            }

            foreach (var bundle in bundleUsed)
            {
                // Sanitize url destination for the bundle
                foreach (var urlDestType in bundle.UrlDestination.Keys)
                {
                    var urlDest = bundle.UrlDestination.GetSafeValue<string>(urlDestType);
                    if (string.IsNullOrWhiteSpace(urlDest))
                    {
                        Site.Warning($"Invalid null or empty url_dest for type [{urlDestType}] in bundle [{bundle.Name}]. Reset default to \"/res/\"");
                        urlDest = "/res/";
                    }

                    Uri url;
                    if (!Uri.TryCreate(urlDest, UriKind.Relative, out url))
                    {
                        Site.Error($"Unable to parse url_dest [{urlDest}] for type [{urlDestType}] in bundle [{bundle.Name}]");
                    }

                    var finalUrl = PathUtil.NormalizeUrl(urlDest, true);

                    if (finalUrl != urlDest)
                    {
                        bundle.UrlDestination[urlDestType] = finalUrl;
                    }
                }

                if (!bundle.Concat && !bundle.Minify)
                {
                    foreach (var link in bundle.Links)
                    {
                        var path = link.Path;
                        var url = link.Url;

                        var outputUrlDirectory = bundle.UrlDestination[link.Type];

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
                            path = PathUtil.NormalizeRelativePath(path, false);

                            var entry = Site.BaseDirectory.CombineToFile(path);

                            ContentObject previousContent;
                            if (staticFiles.TryGetValue(entry, out previousContent))
                            {
                                previousContent.Url = outputUrlDirectory + Path.GetFileName(previousContent.Url);
                            }
                            else
                            {
                                // If the file is private or meta, we need to copy to the output
                                // bool isFilePrivateOrMeta = Site.IsFilePrivateOrMeta(entry.FullName);
                                url = outputUrlDirectory + Path.GetFileName(path);
                                link.Url = url;

                                if (!staticFiles.ContainsKey(entry))
                                {
                                    var newStaticFile = new ContentObject(Site.BaseDirectory, entry, Site) {Url = url};
                                    Site.StaticFiles.Add(newStaticFile);
                                    staticFiles.Add(entry, newStaticFile);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
