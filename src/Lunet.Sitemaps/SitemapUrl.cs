// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;

namespace Lunet.Sitemaps;

public class SitemapUrl : DynamicObject
{
    public SitemapUrl()
    {
    }

    public SitemapUrl(string url)
    {
        Url = url;
    }

    public string? Url
    {
        get => GetSafeValue<string>("loc"); 
        set => SetValue("loc", value);
    }

    public DateTime? LastModified
    {
        get
        {
            var dateStr = GetSafeValue<string>("lastmod");
            return DateTime.TryParse(dateStr, out var date) ? date : null;
        }
        set => SetValue("lastmod", value?.ToString("yyyy-MM-dd"));
    }
        
    public string? ChangeFrequency
    {
        get => GetSafeValue<string>("changefreq");
        set => SetValue("changefreq", value);
    }

    public float? Priority
    {
        get
        {
            var priorityStr = GetSafeValue<string>("priority");
            return float.TryParse(priorityStr, out var priority) ? priority : null;
        }
        set => SetValue("priority", value?.ToString("0.0"));
    }
}
