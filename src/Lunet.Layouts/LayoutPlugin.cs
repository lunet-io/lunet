// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Data;
using Lunet.Core;

namespace Lunet.Layouts;

public class LayoutModule : SiteModule<LayoutPlugin>
{
}

public class LayoutPlugin : SitePlugin
{
    public LayoutPlugin(SiteObject site) : base(site)
    {
        Processor = new LayoutProcessor(this);

        // We insert the layout processor last
        site.Content.ContentProcessors.Add(Processor);
    }

    public LayoutProcessor Processor { get; }
}