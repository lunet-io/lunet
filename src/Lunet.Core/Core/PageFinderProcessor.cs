// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Lunet.Scripts;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core;

public static class UidHelper
{
    public static string Handleize(string uid)
    {
        var tempStringBuilder = new StringBuilder();
        for (int index = 0; index < uid.Length; ++index)
        {
            char c = uid[index];
            if (char.IsLetterOrDigit(c) || c == '.' || c == '-')
            {
                tempStringBuilder.Append(c);
            }
            else
            {
                tempStringBuilder.Append('-');
            }
        }
        if (tempStringBuilder.Length > 0 && tempStringBuilder[tempStringBuilder.Length - 1] == '-')
            --tempStringBuilder.Length;
        string str = tempStringBuilder.ToString();
        return str;
    }
}


/// <summary>
/// A processor to reference all uid used by static, user content and dynamic content.
/// </summary>
public class PageFinderProcessor : ProcessorBase<ContentPlugin>
{
    private readonly Dictionary<string, ContentObject> _mapUidToContent;
    private readonly Dictionary<UPath, ContentObject> _mapPathToContent;
    private readonly Dictionary<string, ExtraContent> _uidExtraContent;
        
    public PageFinderProcessor(ContentPlugin plugin) : base(plugin)
    {
        _mapUidToContent = new Dictionary<string, ContentObject>();
        _mapPathToContent = new Dictionary<UPath, ContentObject>();
        _uidExtraContent = new Dictionary<string, ExtraContent>();

        Site.Builtins.SetValue("xref", DelegateCustomFunction.CreateFunc((Func<string, ScriptObject>)FunctionXRef), true);
        Site.Builtins.SetValue("ref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRef), true);
        Site.Builtins.SetValue("relref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRelRef), true);
    }

    /// <summary>
    /// Tries to find a content object associated with the specified uid.
    /// </summary>
    /// <param name="uid">The uid to look a content object for.</param>
    /// <param name="content">The content object if found.</param>
    /// <returns>`true` if the content with the specified uid was found; `false` otherwise</returns>
    public bool TryFindByUid(string? uid, [NotNullWhen(true)] out ContentObject? content)
    {
        content = null;
        if (uid == null) return false;
        if (_uidExtraContent.TryGetValue(uid, out var extraContent))
        {
            uid = extraContent.DefinitionUid ?? extraContent.Uid;
        }
            
        return _mapUidToContent.TryGetValue(uid, out content);
    }

    public bool TryGetTitleByUid(string uid, out string? title)
    {
        if (TryFindByUid(uid, out var uidContent))
        {
            title = uidContent[PageVariables.XRefName] as string ?? uidContent.Title;
            return true;
        }
        else if (TryGetExternalUid(uid, out var name, out var fullName, out _))
        {
            // For external content, we use fullname.
            title = fullName;
            return true;
        }

        title = null;
        return false;
    }

    public bool TryGetExternalUid(string uid, out string? name, out string? fullname, out string? url)
    {
        if (_uidExtraContent.TryGetValue(uid, out var extraContent))
        {
            uid = extraContent.DefinitionUid ?? extraContent.Uid;
            name = extraContent.Name;
            fullname = extraContent.FullName;
        }
        else
        {
            name = null;
            fullname = null;
        }

        // TODO: make this mapping pluggable via config
        if (uid.StartsWith("System.") || uid.StartsWith("Microsoft."))
        {
            name ??= uid;
            fullname ??= uid;
            url = $"https://docs.microsoft.com/en-us/dotnet/api/{UidHelper.Handleize(uid)}";
            return true;
        }

        fullname = null;
        url = null;
        return false;
    }
        
    public bool TryFindByPath(string path, [NotNullWhen(true)] out ContentObject? content)
    {
        return _mapPathToContent.TryGetValue(path, out content);
    }

    public override void Process(ProcessingStage stage)
    {
        foreach (var page in Site.StaticFiles)
        {
            _mapPathToContent[page.Path] = page;
            RegisterUid(page);
        }

        foreach (var page in Site.Pages)
        {
            _mapPathToContent[page.Path] = page;
            RegisterUid(page);
        }
            
        foreach (var page in Site.DynamicPages)
        {
            _mapPathToContent[page.Path] = page;
            RegisterUid(page);
        }
    }

    public void RegisterExtraContent(ExtraContent extraContent)
    {
        if (extraContent == null) throw new ArgumentNullException(nameof(extraContent));
        if (extraContent.Uid == null) throw new ArgumentException("The uid of this extra content cannot be null", nameof(extraContent));
        _uidExtraContent[extraContent.Uid] = extraContent;
    }

    private void RegisterUid(ContentObject page)
    {
        var uid = page.Uid;
        if (string.IsNullOrEmpty(uid)) return;
                
        if (_mapUidToContent.TryGetValue(uid, out var content))
        {
            if (!ReferenceEquals(content, page))
            {
                Site.Error($"Duplicated uid `{uid}` used. The content {(page.Path.IsNull ? page.Url : (string) page.Path)} has the same uid than {(content.Path.IsNull ? content.Url : (string) content.Path)}");
            }
        }
        else
        {
            _mapUidToContent.Add(uid, page);
        }
    }


    private ScriptObject FunctionXRef(string uid)
    {
        if (uid == null) return null!;

        if (TryFindByUid(uid, out var uidContent))
        {
            var name = uidContent[PageVariables.XRefName] as string ?? uidContent.Title;
            var fullName = uidContent[PageVariables.XRefFullName] as string ?? name;
            return new ScriptObject()
            {
                {"url", uidContent.Url},
                {"name", name},
                {"fullname", fullName},
                {"page", uidContent },
            };
        }

        if (TryGetExternalUid(uid, out var externalName, out var externalFullName, out var url))
        {
            // TODO: add friendly name and fullname for an external uid using References
            return new ScriptObject()
            {
                {"url", url},
                {"name", externalName},
                {"fullname", externalFullName},
            };
        }

        return null!;
    }

    private string UrlRef(TemplateContext context, string url)
    {
        return UrlRef(context is LunetTemplateContext lunetContext ? lunetContext.Page : null, url);
    }

    public string UrlRef(ContentObject? fromPage, string url)
    {
        return UrlRef(fromPage, url, false);
    }

    private string UrlRelRef(TemplateContext context, string url)
    {
        return UrlRelRef(context is LunetTemplateContext lunetContext ? lunetContext.Page : null, url);
    }

    public string UrlRelRef(ContentObject? fromPage, string url)
    {
        return UrlRef(fromPage, url, true);
    }
        
    private string UrlRef(ContentObject? page, string? url, bool rel)
    {
        url ??= "/";

        var baseUrl = Site.BaseUrl;
        var basePath = Site.BasePath;

        // In case of using URL on an external URL (https:), don't error but return it as it is
        if (url.Contains(':'))
        {
            if (url.StartsWith("xref:"))
            {
                var xref = url.Substring("xref:".Length);
                if (TryFindByUid(xref, out var pageUid))
                {
                    url = pageUid.Url ?? string.Empty;
                    return rel ? url : (string)(UPath)$"{baseUrl}/{(basePath ?? string.Empty)}/{url}";
                }

                if (TryGetExternalUid(xref, out _, out _, out url))
                {
                    return url ?? string.Empty;
                }

                Site.Warning($"Unable to find xref {xref} in page {page?.Url}");
            }
                
            return url ?? string.Empty;
        }

        // Validate the url
        if (!UPath.TryParse(url, out var urlPath))
        {
            throw new ArgumentException($"Malformed url `{url}`", nameof(url));
        }

        UPath absPath = url;
        // If the URL is not absolute, we make it absolute from the current page
        if (absPath.IsRelative)
        {
            if (page?.Url != null)
            {
                var directory = page.Path.GetDirectory();
                absPath = (string)(directory / urlPath);
            }
            else
            {
                throw new ArgumentException($"Invalid url `{url}`. Expecting an absolute url starting with /", nameof(url));
            }
        }
        // Resolve the page
        if (_mapPathToContent.TryGetValue(absPath, out var pageLink))
        {
            var destPath = pageLink.GetDestinationPath();

            var newUrl = (string)destPath;
            if (newUrl.EndsWith("/index.html") || newUrl.EndsWith("/index.htm"))
            {
                newUrl = (string)destPath.GetDirectory();
            }
            absPath = newUrl;
        }

        if (!string.IsNullOrEmpty(basePath))
        {
            // Normalize base path
            if (!basePath.StartsWith('/'))
            {
                basePath = "/" + basePath;
            }

            if (basePath.EndsWith('/'))
            {
                basePath = basePath.TrimEnd('/');
            }

            absPath = UPath.Combine(basePath, "." + absPath);
        }

        var finalUrl = $"{baseUrl}{absPath}";

        if (!Uri.TryCreate(finalUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid url `{finalUrl}`.", nameof(url));
        }
        
        return rel
            ? $"{absPath}{(!string.IsNullOrEmpty(uri.Query) ? $"?{uri.Query}" : string.Empty)}"
            : $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? string.Empty : $":{uri.Port.ToString(CultureInfo.InvariantCulture)}")}{absPath}{(!string.IsNullOrEmpty(uri.Query) ? $"?{uri.Query}" : string.Empty)}";
    }
}
