// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Zio;

namespace Lunet.Bundles;

public class BundleLink : DynamicObject<BundleObject>
{
    public BundleLink(BundleObject parent, string type, string? path, string? url, string? urlWithoutBasePath, string? mode) : base(parent)
    {
        Type = type;
        Path = path;
        Url = url;
        UrlWithoutBasePath = urlWithoutBasePath;
        Mode = mode ?? "";
    }

    public string Type
    {
        get => GetSafeValue<string>("type") ?? string.Empty;
        set => this["type"] = value;
    }


    public string? Path
    {
        get => GetSafeValue<string>("path");
        set => this["path"] = value;
    }

    public string? Url
    {
        get => GetSafeValue<string>("url");
        set => this["url"] = value;
    }
    
    public string? UrlWithoutBasePath
    {
        get => GetSafeValue<string>("url_without_basepath");
        set => this["url_without_basepath"] = value;
    }

    public string Mode
    {
        get => GetSafeValue<string>("mode") ?? "";
        set => this["mode"] = value;
    }

    public string? Content { get; set; }

    public ContentObject? ContentObject { get; set; }
}
