using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Lunet.Scripts;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    public class BuiltinsObject : DynamicObject<SiteObject>, ISiteProcessor
    {
        private readonly Dictionary<UPath, ContentObject> _pages;

        public string Name => SiteVariables.Builtins;

        public SiteObject Site { get; }

        public BuiltinsObject(SiteObject parent) : base(parent)
        {
            Site = parent;
            _pages = new Dictionary<UPath, ContentObject>();
            parent.SetValue(SiteVariables.Builtins, this, true);
            Head = parent.Scripts.CompileAnonymous("include 'builtins/head.sbn-html'");

            // Helpers used for declaring panels (e.g {{NOTE do}}This is a note.{{end}}
            var helpers = @"
# Defines the generic alert helper function
func ALERT
    `<div class='` + (($0 + ` ` + $.class) | string.rstrip ) + `' role='alert'>`
        `<div class='` + $0 + `-heading'>`
            `<span class='` + $0 + `-icon'></span><span class='` + $0 + `-heading-text'></span>`
        `</div>`
        `<div class='` + $0 + `-content'>` + '\n\n'
            $1
        `</div>`
    '</div>\n\n'
end

# Defines alert functions
func NOTE; ALERT 'lunet-alert-note' class:$.class @$0; end
func TIP; ALERT 'lunet-alert-tip' class:$.class @$0; end
func WARNING; ALERT 'lunet-alert-warning' class:$.class @$0; end
func IMPORTANT; ALERT 'lunet-alert-important' class:$.class @$0; end
func CAUTION; ALERT 'lunet-alert-caution' class:$.class @$0; end
func CALLOUT; ALERT 'lunet-alert-callout' class:$.class @$0; end
";

            parent.Scripts.TryImportScript(helpers, "internal_helpers", this, ScriptFlags.AllowSiteFunctions, out _);
            SetValue("ref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRef), true);
            SetValue("relref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRelRef), true);

            // Add our own to_rfc822
            var dateTimeFunctions = (DateTimeFunctions) Site.Scripts.Builtins["date"];
            dateTimeFunctions.Import("to_rfc822", (Func<DateTime, string>)ToRFC822);

            Site.Content.AfterLoadingProcessors.Add(this);
        }

        public object Head
        {
            get => GetSafeValue<object>("Head");
            set => SetValue("Head", value);
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

            // In case of using URL on an external URL (https:), don't error but return it as it is
            if (url.Contains(":"))
            {
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

            var baseUrl = Site.BaseUrl;
            var basePath = Site.BasePath;
            var urlToValidate = $"{baseUrl}/{(basePath ?? string.Empty)}/{url}";
            if (!Uri.TryCreate(urlToValidate, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException($"Invalid url `{urlToValidate}`.", nameof(url));
            }

            UPath.TryParse(uri.AbsolutePath, out var absPath);

            // Resolve the page
            if (_pages.TryGetValue(absPath, out var pageLink))
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

        void ISiteProcessor.Process(ProcessingStage stage)
        {
            Debug.Assert(stage == ProcessingStage.AfterLoadingContent);

            foreach (var page in Site.Pages)
            {
                _pages[page.Path] = page;
            }

            foreach (var page in Site.StaticFiles)
            {
                _pages[page.Path] = page;
            }
        }
        
        private static string ToRFC822(DateTime date)
        {
            int offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
            string timeZone = "+" + offset.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            if (offset < 0)
            {
                int i = offset * -1;
                timeZone = "-" + i.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            }
            return date.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'));
        }
    }
}