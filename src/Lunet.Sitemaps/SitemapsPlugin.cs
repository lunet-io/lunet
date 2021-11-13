// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Sitemaps;

public class SitemapsModule : SiteModule<SitemapsPlugin>
{
}

public class SitemapsPlugin : SitePlugin
{
    public SitemapsPlugin(SiteObject site) : base(site)
    {
        Enable = true;
        var processor = new SitemapsProcessor(this);
        site.Content.BeforeLoadingProcessors.Add(processor);
        site.Content.BeforeProcessingProcessors.Add(processor);
        site.Content.AfterRunningProcessors.Add(processor);
    }

    public bool Enable
    {
        get => GetSafeValue<bool>("enable");
        set => SetValue("enable", value);
    }
}