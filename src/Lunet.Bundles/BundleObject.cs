// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Resources;
using Scriban.Runtime;
using Zio;

namespace Lunet.Bundles;

public class BundleObject : DynamicObject<BundlePlugin>
{
    public BundleObject(BundlePlugin plugin, string name) : base(plugin)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Name = name;
        Links = new List<BundleLink>();

        SetValue(BundleObjectProperties.Name, Name, true);
        SetValue(BundleObjectProperties.Links, Links, true);

        UrlDestination = new DynamicObject<BundleObject>(this)
        {
            [BundleObjectProperties.JsType] = "/js/",
            [BundleObjectProperties.CssType] = "/css/"
        };
        SetValue(BundleObjectProperties.UrlDestination, UrlDestination, true);
        MinifyExtension = ".min";

        this.Import(BundleObjectProperties.JsType, (Action<object, string, string>)AddJs);
        this.Import(BundleObjectProperties.CssType, (Action<object, string>)AddCss);
        this.Import(BundleObjectProperties.ContentType, (Action<object, string, string>)AddContent);
    }

    public string Name { get; }

    public List<BundleLink> Links { get; }
        
    public ScriptObject UrlDestination { get; }

    public bool Concat
    {
        get { return GetSafeValue<bool>(BundleObjectProperties.Concat); }
        set { this[BundleObjectProperties.Concat] = value; }
    }

    public bool Minify
    {
        get { return GetSafeValue<bool>(BundleObjectProperties.Minify); }
        set { this[BundleObjectProperties.Minify] = value; }
    }

    public string MinifyExtension
    {
        get { return GetSafeValue<string>(BundleObjectProperties.MinifyExtension) ?? ".min"; }
        set { this[BundleObjectProperties.MinifyExtension] = value; }
    }

    public string? Minifier
    {
        get { return GetSafeValue<string>(BundleObjectProperties.Minifier); }
        set { this[BundleObjectProperties.Minifier] = value; }
    }

    public void AddLink(string type, string path, string? url = null, string? mode = null)
    {
        InsertLink(Links.Count, type, path, url, mode);
    }

    public void InsertLink(int index, string type, string path, string? url = null, string? mode = null)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (path == null) throw new ArgumentNullException(nameof(path));

        if (!UPath.TryParse(path, out _))
        {
            throw new ArgumentException($"Invalid path [{path}]", nameof(path));
        }

        if (url != null)
        {
            if (!UPath.TryParse(url, out _))
            {
                throw new ArgumentException($"Invalid url [{url}]", nameof(url));
            }
        }

        if (path.Contains("*") && (url == null || !url.EndsWith("/")))
        {
            throw new ArgumentException($"Invalid url [{path}]. Must end with a `/` if the path contains a wildcard.", nameof(url));
        }

        string? urlBasePath = null;
        if (url is not null)
        {
            urlBasePath = url;
            url = new UPath($"{this.Parent.Site.BasePath}/{url}").FullName;
        }
        var link = new BundleLink(this, type, path, url, urlBasePath, mode);
        Links.Insert(index, link);
    }

    public void AddJs(object resourceOrPath, string? path = null, string mode = "defer")
    {
        AddLink(BundleObjectProperties.JsType, resourceOrPath, path, mode: mode);
    }

    public void AddCss(object resourceOrPath, string? path = null)
    {
        if (resourceOrPath == null) throw new ArgumentNullException(nameof(resourceOrPath));
        AddLink(BundleObjectProperties.CssType, resourceOrPath, path);
    }

    public void AddContent(object resourceOrPath, string pathOrUrl, string? url = null)
    {
        AddLink(BundleObjectProperties.ContentType, resourceOrPath, resourceOrPath is string ? null : pathOrUrl, resourceOrPath is string ? pathOrUrl : url);
    }
        
    private void AddLink(string kind, object resourceOrPath, string? path, string? url = null, string? mode = null)
    {
        if (resourceOrPath == null) throw new ArgumentNullException(nameof(resourceOrPath));
        if (resourceOrPath is ResourceObject resource)
        {
            if (path != null)
            {
                if (!UPath.TryParse(path, out var relativePath))
                {
                    throw new ArgumentException($"Invalid path {path}.", nameof(path));
                }
                    
                path = (string)(resource.Path / relativePath.ToRelative());
            }
            else
            {
                path ??= resource["main"]?.ToString();
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path), "path cannot be null with a resource");
                }
            }
        }
        else if (resourceOrPath is string str)
        {
            if (path != null)
            {
                throw new ArgumentException("Parameter must be null if first argument is already a path.", nameof(path));
            }
                
            path = str;
        }
        else
        {
            throw new ArgumentException( $"Invalid parameter type ({resourceOrPath?.GetType()}) for {kind} function.", nameof(resourceOrPath));
        }
            
        AddLink(kind, path, url, mode);
    }
}
