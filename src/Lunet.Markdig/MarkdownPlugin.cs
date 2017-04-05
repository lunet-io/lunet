// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

// Register this plugin

namespace Lunet.Markdown
{
    public class MarkdownPlugin : SitePlugin
    {
        public MarkdownPlugin(SiteObject site) : base(site)
        {
            site.Content.BeforeLoadingProcessors.Add(new MarkdownProcessor(this));
        }
    }
}