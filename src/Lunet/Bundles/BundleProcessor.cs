// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Plugins;

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
                if (!bundle.Concat && !bundle.Minify)
                {
                    foreach (var link in bundle.Links)
                    {
                        var url = link.Url;

                        var outputUrlDirectory = bundle.Directories[link.Type];
                        // TODO: if dir is null, what can we do?

                        Uri result;
                        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out result))
                        {
                            if (result.IsAbsoluteUri)
                            {
                                // TODO: handle absolute Uri                                
                            }
                            else
                            {
                                url = PathUtil.NormalizeRelativePath(url, false);

                                var entry = new FileInfo(Path.Combine(Site.BaseDirectory, url)).Normalize();

                                ContentObject previousContent;
                                if (staticFiles.TryGetValue(entry, out previousContent))
                                {
                                    previousContent.Url = outputUrlDirectory + Path.GetFileName(previousContent.Url);
                                }
                                else
                                {
                                    // If the file is private or meta, we need to copy to the output
                                    // bool isFilePrivateOrMeta = Site.IsFilePrivateOrMeta(entry.FullName);
                                    url = outputUrlDirectory + Path.GetFileName(url);
                                    link.Url = url;

                                    if (!staticFiles.ContainsKey(entry))
                                    {
                                        var newStaticFile = new ContentObject(Site.BaseDirectory, entry, Site) { Url = url };
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
}