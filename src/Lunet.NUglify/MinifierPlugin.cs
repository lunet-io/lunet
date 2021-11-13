﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Bundles;
using Lunet.Core;
using NUglify;

// Register this plugin

namespace Lunet.Minifiers;

public class MinifierModule : SiteModule<MinifierPlugin>
{
}

public class MinifierPlugin : SitePlugin, IContentMinifier
{
    public MinifierPlugin(SiteObject site, BundlePlugin bundlePlugin) : base(site)
    {
        if (bundlePlugin == null) throw new ArgumentNullException(nameof(bundlePlugin));
        bundlePlugin.BundleProcessor.Minifiers.AddIfNotAlready(this);
    }

    public string Minify(string type, string content, string contentPath)
    {
        // TODO: handle filenames, options...etc.
        UglifyResult result;
        if (type == "js")
        {
            result = Uglify.Js(content, contentPath);
        }
        else if (type == "css")
        {
            result = Uglify.Css(content, contentPath);
        }
        else
        {
            return content;
        }

        if (result.HasErrors)
        {
            foreach (var error in result.Errors)
            {
                error.Message = $"Minifying error: {error.Message}";
                Site.Warning(error.ToString());
            }

            return content;
        }

        return result.Code;
    }
}