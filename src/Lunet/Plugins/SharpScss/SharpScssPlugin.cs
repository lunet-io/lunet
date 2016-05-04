// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using Lunet.Core;
using Lunet.Plugins;
using Lunet.Plugins.SharpScss;

// Register this plugin
[assembly: SitePlugin(typeof(SharpScssPlugin))]

namespace Lunet.Plugins.SharpScss
{

    public class SharpScssPlugin : ISitePlugin
    {
        public string Name => "sharpscss";

        public void Initialize(SiteObject site)
        {
            site.DynamicObject.SetValue("scss", new ScssObject(this), true);
            site.Plugins.Processors.Add(new ScssProcessor());
        }
    }
}