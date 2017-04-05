// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Scss
{
    public class ScssPlugin : SitePlugin
    {
        public ScssPlugin(SiteObject site) : base(site)
        {
            site.SetValue("scss", new ScssObject(this), true);
            site.Content.ContentProcessors.Add(new ScssProcessor(this));
        }
    }
}