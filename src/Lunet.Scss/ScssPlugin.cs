// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Scss
{
    public class ScssModule : SiteModule<ScssPlugin>
    {
    }

    public class ScssPlugin : SitePlugin
    {
        public ScssPlugin(SiteObject site) : base(site)
        {
            Includes = new PathCollection();
            SetValue("includes", Includes, true);
            site.SetValue("scss", this, true);
            site.Content.ContentProcessors.Add(new ScssProcessor(this));
        }

        public override string Name => "scss";

        public PathCollection Includes { get; }
    }
}