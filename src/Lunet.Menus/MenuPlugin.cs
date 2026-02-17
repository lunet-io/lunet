// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Concurrent;
using Lunet.Bundles;
using Lunet.Core;
using Zio;

namespace Lunet.Menus;

public class MenuModule : SiteModule<MenuPlugin>
{
}

public class MenuPlugin : SitePlugin
{
    public MenuPlugin(SiteObject site, BundlePlugin bundlePlugin) : base(site)
    {
        BundlePlugin = bundlePlugin;
        Processor = new MenuProcessor(this);
        Site.SetValue("menu", this, true);

        // Per-build cache of generated async menu partials. Must be initialized before any parallel page rendering.
        if (Site.GetSafeValue<ConcurrentDictionary<UPath, string>>(MenuAsyncPartialsWriter.SiteKey) is null)
        {
            Site.SetValue(MenuAsyncPartialsWriter.SiteKey, new ConcurrentDictionary<UPath, string>(), true);
        }

        Site.Content.AfterRunningProcessors.Insert(0, Processor);
        Site.Content.BeforeProcessingProcessors.Insert(0, Processor);
        Site.Content.AfterProcessingProcessors.Insert(0, new MenuAsyncPartialsWriter(this));
    }

    internal BundlePlugin BundlePlugin { get; }

    public MenuProcessor Processor { get; }

    /// <summary>
    /// If a rendered menu has at least this many items, it is emitted as a hashed partial HTML file and loaded at runtime.
    /// Set to 0 to disable async menu loading.
    /// </summary>
    public int AsyncLoadThreshold
    {
        get
        {
            if (!ContainsKey("async_load_threshold"))
            {
                return 10;
            }

            var value = GetSafeValue<int>("async_load_threshold");
            return Math.Max(0, value);
        }
        set => SetValue("async_load_threshold", Math.Max(0, value), true);
    }

    /// <summary>
    /// Output folder (URL path) for cached menu partials. Must be an absolute URL path (e.g. <c>/partials/menus</c>).
    /// </summary>
    public string AsyncPartialsFolder
    {
        get => GetSafeValue<string>("async_partials_folder") ?? "/partials/menus";
        set => SetValue("async_partials_folder", value);
    }

    public void RegisterMenu(string name, MenuObject menu, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(menu);

        if (!overwrite && ContainsKey(name))
        {
            return;
        }

        menu.Name ??= name;
        SetValue(name, menu);
    }

    public void SetPageMenu(ContentObject page, MenuObject menu, bool force = false)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(menu);

        Processor.SetPageMenu(page, menu, force);
    }
}
