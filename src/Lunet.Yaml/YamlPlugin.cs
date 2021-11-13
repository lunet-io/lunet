// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Datas;

namespace Lunet.Yaml;

public class YamlModule : SiteModule<YamlPlugin>
{
}

public class YamlPlugin : SitePlugin
{
    public YamlPlugin(SiteObject site, DatasPlugin datasPlugin) : base(site)
    {
        if (datasPlugin == null) throw new ArgumentNullException(nameof(datasPlugin));
        datasPlugin.DataLoaders.Add(new YamlDataLoader());
        site.Scripts.FrontMatterParsers.Add(new YamlFrontMatterParser());
    }
}