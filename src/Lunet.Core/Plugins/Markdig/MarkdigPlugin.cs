// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.IO;
using Lunet.Core;
using Lunet.Plugins;
using Lunet.Plugins.Markdig;

// Register this plugin
[assembly: SitePlugin(typeof(MarkdigPlugin))]

namespace Lunet.Plugins.Markdig
{
    public class MarkdigPlugin : ISitePlugin
    {
        public string Name => "markdig";

        public void Initialize(SiteObject site)
        {
            site.Builder.PreProcessors.AddIfNotAlready(new MarkdigProcessor());
        }
    }
}