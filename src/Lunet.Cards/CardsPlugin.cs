// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Cards;

public class CardsModule : SiteModule<CardsPlugin>
{
}

/// <summary>
/// Manages resources.
/// </summary>
public class CardsPlugin : SitePlugin
{
    public CardsPlugin(SiteObject site) : base(site)
    {
        Site.SetValue("cards", this, true);

        Twitter = new TwitterCards(this);
        Og = new OgCards(this);

        // Add the bundle builtins to be included by default in site.html.head.includes
        Site.Html.Head.Includes.Add("_builtins/cards.sbn-html");
    }

    public TwitterCards Twitter { get; }

    public OgCards Og { get; }
}