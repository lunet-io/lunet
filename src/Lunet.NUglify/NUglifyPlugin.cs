// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Bundles;
using Lunet.Core;
using Lunet.Plugins;
using Lunet.Plugins.NUglify;
using NUglify;

// Register this plugin
[assembly: SitePlugin(typeof(NUglifyPlugin))]

namespace Lunet.Plugins.NUglify
{

    public class NUglifyPlugin : ISitePlugin, IContentMinifier
    {
        public string Name => "nuglify";
        public void Initialize(SiteObject site)
        {
            var bundleProcessor = site.Builder.Processors.Find<BundleProcessor>();
            if (bundleProcessor != null)
            {
                bundleProcessor.Minifiers.AddIfNotAlready(this);
            }
        }

        public string Minify(string type, string content)
        {
            // TODO: handle filenames, options...etc.
            UgliflyResult? result;
            if (type == "js")
            {
                result = Uglify.Js(content);
            }
            else if (type == "css")
            {
                result = Uglify.Css(content);
            }
            else
            {
                return content;
            }

            return !result.Value.HasErrors ? result.Value.Code : content;
        }
    }
}