// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Menus;

public class MenuModule : SiteModule<MenuPlugin>
{
}

public class MenuPlugin : SitePlugin
{
    public MenuPlugin(SiteObject site) : base(site)
    {
        Processor = new MenuProcessor(this);
        Site.SetValue("menu", this, true);
        Site.Content.AfterRunningProcessors.Insert(0, Processor);
        Site.Content.BeforeProcessingProcessors.Insert(0, Processor);
        HomeTitle = "Home";
    }
    
    public string HomeTitle
    {
        get => GetSafeValue<string>("home_title");
        set => SetValue("home_title", value);
    }

    public MenuProcessor Processor { get; }
}