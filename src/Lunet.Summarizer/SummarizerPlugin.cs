// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using System.Diagnostics;

namespace Lunet.Summarizer;

public class SummarizerModule : SiteModule<SummarizerPlugin>
{
}
    
public class SummarizerPlugin : SitePlugin
{
    public SummarizerPlugin(SiteObject site) : base(site)
    {
        var processor = new SummarizerProcessor(this);

        // Run the summarizer processor after the markdown only
        site.Content.ContentProcessors.Insert(0, processor);
    }
}