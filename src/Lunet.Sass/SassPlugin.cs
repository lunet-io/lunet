// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Lunet.Layouts;
using Zio;

namespace Lunet.Sass;

public class SassModule : SiteModule<SassPlugin>
{
}

public class SassPlugin : SitePlugin, ILayoutConverter
{
    public static readonly ContentType ScssType = new ContentType("scss");

    public SassPlugin(SiteObject site, LayoutPlugin layoutPlugin) : base(site)
    {
        Includes = new PathCollection();
        SetValue("includes", Includes, true);
        site.SetValue("scss", this, true);
        site.SetValue("sass", this, true);
        layoutPlugin.Processor.RegisterConverter(ScssType, this);
    }

    public override string Name => "scss";

    public PathCollection Includes { get; }

    public bool UseDartSass
    {
        get => GetSafeValue<bool>("use_dart_sass");
        set => SetValue("use_dart_sass", value, false);
    }

    public bool ShouldConvertIfNoLayout => true;

    public void Convert(ContentObject file)
    {
        var contentType = file.ContentType;

        // This plugin is only working on scss files
        if (contentType != ScssType)
        {
            return;
        }
        var content = file.GetOrLoadContent();

        var includePaths = new List<DirectoryEntry>();
        foreach (var pathObj in Includes)
        {
            var path = pathObj as string;
            if (path != null && UPath.TryParse(path, out var validPath) && Site.MetaFileSystem.DirectoryExists(validPath))
            {
                includePaths.Add(new DirectoryEntry(Site.MetaFileSystem, validPath));
            }
            else
            {
                Site.Error($"Invalid folder path `{pathObj}` found in site.scss.includes.");
            }
        }


        if (UseDartSass)
        {
            DartSassTransform.Convert(file, includePaths, Site);
        }
        else
        {
            LibSassTransform.Convert(file, includePaths, Site);
        }
    }
}