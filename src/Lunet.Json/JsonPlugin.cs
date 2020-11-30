// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Datas;

namespace Lunet.Json
{
    public class JsonPlugin : SitePlugin
    {
        public JsonPlugin(SiteObject site, DatasPlugin dataPlugin) : base(site)
        {
            if (dataPlugin == null) throw new ArgumentNullException(nameof(dataPlugin));
            dataPlugin.DataLoaders.Add(new JsonDataLoader());
        }
    }
}