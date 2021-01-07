// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lunet.Scripts;
using Scriban;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    /// <summary>
    /// A processor to reference all uid used by static, user content and dynamic content.
    /// </summary>
    public class PageFinderProcessor : ProcessorBase<ContentPlugin>
    {
        private readonly Dictionary<string, ContentObject> _mapUidToContent;
        private readonly Dictionary<UPath, ContentObject> _mapPathToContent;
        
        public PageFinderProcessor(ContentPlugin plugin) : base(plugin)
        {
            _mapUidToContent = new Dictionary<string, ContentObject>();
            _mapPathToContent = new Dictionary<UPath, ContentObject>();
            
            Site.Builtins.SetValue("xref", DelegateCustomFunction.CreateFunc((Func<string, ContentObject>)FunctionXRef), true);
            Site.Builtins.SetValue("ref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRef), true);
            Site.Builtins.SetValue("relref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRelRef), true);
        }

        /// <summary>
        /// Tries to find a content object associated with the specified uid.
        /// </summary>
        /// <param name="uid">The uid to look a content object for.</param>
        /// <param name="content">The content object if found.</param>
        /// <returns>`true` if the content with the specified uid was found; `false` otherwise</returns>
        public bool TryFindByUid(string uid, out ContentObject content)
        {
            return _mapUidToContent.TryGetValue(uid, out content);
        }
        
        public bool TryFindByPath(string path, out ContentObject content)
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

        public void RegisterUid(ContentObject page)
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

        private ContentObject FunctionXRef(string uid)
        {
            if (uid == null) return null;
            TryFindByUid(uid, out var result);
            return result;
        }
        
                private string UrlRef(TemplateContext context, string url)
        {
            return UrlRef(context is LunetTemplateContext lunetContext ? lunetContext.Page : null, url);
        }

        public string UrlRef(ContentObject fromPage, string url)
        {
            return UrlRef(fromPage, url, false);
        }

        private string UrlRelRef(TemplateContext context, string url)
        {
            return UrlRelRef(context is LunetTemplateContext lunetContext ? lunetContext.Page : null, url);
        }

        public string UrlRelRef(ContentObject fromPage, string url)
        {
            return UrlRef(fromPage, url, true);
        }
        
        private string UrlRef(ContentObject page, string url, bool rel)
        {
            url ??= "/";

            var baseUrl = Site.BaseUrl;
            var basePath = Site.BasePath;

            // In case of using URL on an external URL (https:), don't error but return it as it is
            if (url.Contains(":"))
            {
                if (url.StartsWith("xref:"))
                {
                    if (TryFindByUid(url.Substring("xref:".Length), out var pageUid))
                    {
                        url = pageUid.Url;
                        return rel ? url : (string) (UPath) $"{baseUrl}/{(basePath ?? string.Empty)}/{url}";
                    }
                }
                
                return url;
            }

            // Validate the url
            if (!UPath.TryParse(url, out var urlPath))
            {
                throw new ArgumentException($"Malformed url `{url}`", nameof(url));
            }

            // If the URL is not absolute, we make it absolute from the current page
            if (!url.StartsWith("/"))
            {
                if (page?.Url != null)
                {
                    var directory = page.GetDestinationDirectory();
                    url = (string)(directory / urlPath);
                }
                else
                {
                    throw new ArgumentException($"Invalid url `{url}`. Expecting an absolute url starting with /", nameof(url));
                }
            }

            if (!UPath.TryParse(url, out _))
            {
                throw new ArgumentException($"Malformed url `{url}`", nameof(url));
            }

            var urlToValidate = $"{baseUrl}/{(basePath ?? string.Empty)}/{url}";
            if (!Uri.TryCreate(urlToValidate, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException($"Invalid url `{urlToValidate}`.", nameof(url));
            }

            UPath.TryParse(uri.AbsolutePath, out var absPath);

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

            return rel
                ? $"{absPath}{(!string.IsNullOrEmpty(uri.Query) ? $"?{uri.Query}" : string.Empty)}"
                : $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? string.Empty : $":{uri.Port.ToString(CultureInfo.InvariantCulture)}")}{absPath}{(!string.IsNullOrEmpty(uri.Query) ? $"?{uri.Query}" : string.Empty)}";
        }
    }
}