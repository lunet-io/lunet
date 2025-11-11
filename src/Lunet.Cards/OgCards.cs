// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Cards;

/// <summary>
/// Represents Open Graph (OG) metadata for a web page, including properties such as type, URL, and locale.
/// </summary>
/// <remarks>This class is used to define Open Graph metadata for a web page, which can be consumed by social
/// media platforms and other services to display rich previews of the page. The default <see cref="Type"/> is set to
/// "article", but it can be customized as needed (e.g., "website" for a homepage).</remarks>
public class OgCards : CardsBase
{
    public OgCards(CardsPlugin parent) : base("og", parent)
    {
        // Default is article but the frontpage will prefer to specify website
        Type = "article";
    }
    
    // <meta property="og:type" content="website">
    public string Type
    {
        get => GetSafeValue<string>("type");
        set => SetValue("type", value);
    }
    // <meta property="og:url" content="https://lunet.io/">
    public string Url
    {
        get => GetSafeValue<string>("url");
        set => SetValue("url", value);
    }
    // <meta property="og:locale" content="en_US">
    public string Locale
    {
        get => GetSafeValue<string>("locale");
        set => SetValue("locale", value);
    }
}