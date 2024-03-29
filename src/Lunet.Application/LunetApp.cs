﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Threading;
using Lunet.Api;
using Lunet.Api.DotNet;
using Lunet.Attributes;
using Lunet.Bundles;
using Lunet.Cards;
using Lunet.Core;
using Lunet.Datas;
using Lunet.Extends;
using Lunet.Helpers;
using Lunet.Json;
using Lunet.Layouts;
using Lunet.Markdown;
using Lunet.Menus;
using Lunet.Minifiers;
using Lunet.Resources;
using Lunet.Rss;
using Lunet.Taxonomies;
using Lunet.Scss;
using Lunet.Search;
using Lunet.Server;
using Lunet.Sitemaps;
using Lunet.Summarizer;
using Lunet.Toml;
using Lunet.Tracking;
using Lunet.Watcher;
using Lunet.Yaml;

namespace Lunet;

public class LunetApp
{
    public LunetApp(SiteConfiguration config = null)
    {
        Config = config ?? new SiteConfiguration();
        Modules = new OrderedList<SiteModule>()
        {
            new ApiModule(),
            new BundleModule(),
            new ApiDotNetModule(),
            new MenuModule(),
            new ExtendsModule(),
            new SummarizerModule(),
            new MarkdownModule(),
            new LayoutModule(),
            new ResourceModule(),
            new DatasModule(),
            new ServerModule(),
            new WatcherModule(),
            new MinifierModule(),
            new RssModule(),
            new ScssModule(),
            new TaxonomyModule(),
            new CardsModule(),
            new SearchModule(),
            new SitemapsModule(),
            new AttributesModule(),
            new YamlModule(),
            new JsonModule(),
            new TomlModule(),
            new TrackingModule(),
        };
    }

    public SiteConfiguration Config { get; }

    public OrderedList<SiteModule> Modules { get; }

    public int Run(CancellationTokenSource cancellationTokenSource, params string[] args)
    {
        if (cancellationTokenSource == null) throw new ArgumentNullException(nameof(cancellationTokenSource));
        // The order modules are registered here is important
        var app = new SiteApplication(Config);
        foreach (var module in Modules)
        {
            app.Add(module);
        }

        try
        {
            app.Execute(args);

            var runner = new SiteRunner(app.Config);
            return runner.Run(cancellationTokenSource);
        }
        catch (Exception ex)
        {
            app.Config.Error(ex, ex.Message);
            return 1;
        }
        finally
        {
            app.Config.LoggerFactory.Dispose();
        }
    }

    public int Run(params string[] args)
    {
        return Run(new CancellationTokenSource(), args);
    }
}