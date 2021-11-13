// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Lunet.Layouts;
using Zio;

namespace Lunet.Rss;

public class RssModule : SiteModule<RssPlugin>
{
}

public class RssPlugin : SitePlugin
{
    public RssPlugin(SiteObject site) : base(site)
    {
        Site.Content.LayoutTypes.AddListType("rss");
        site.SetValue("rss", this, true);
        Limit = 10;
    }

    public int Limit
    {
        get => GetSafeValue<int>("limit");
        set => SetValue("limit", value, false);
    }
}