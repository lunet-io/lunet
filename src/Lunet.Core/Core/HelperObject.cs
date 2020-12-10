using System;
using System.Globalization;
using System.Text;
using Lunet.Scripts;
using Scriban;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    public class HelperObject : DynamicObject<SiteObject>
    {
        public HelperObject(SiteObject parent) : base(parent)
        {
            parent.SetValue(SiteVariables.Helpers, this, true);
            Head = parent.Scripts.CompileAnonymous("include 'builtins/head.sbn-html'");
            SetValue("ref", DelegateCustomFunction.CreateFunc((Func<TemplateContext, string, string>)UrlRef), true);

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
        }

        public object Head
        {
            get => GetSafeValue<object>("Head");
            set => SetValue("Head", value);
        }

        public string UrlRef(TemplateContext context, string url)
        {
            url ??= "/";

            // In case of using url on an external URL, don't error but return it as it is
            if (url.StartsWith("http:") || url.StartsWith("https:") || url.StartsWith("ftp:"))
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
                if (context is LunetTemplateContext lunetContext && lunetContext.Page?.Url != null)
                {
                    var directory = lunetContext.Page.GetDestinationDirectory();
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

            var baseUrl = Parent.BaseUrl;
            var basePath = Parent.BasePath;
            var urlToValidate = $"{baseUrl}/{(basePath ?? string.Empty)}/{url}";
            if (!Uri.TryCreate(urlToValidate, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException($"Invalid url `{urlToValidate}`.", nameof(url));
            }

            UPath.TryParse(uri.AbsolutePath, out var absPath);
            
            var fullUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? string.Empty : $":{uri.Port.ToString(CultureInfo.InvariantCulture)}")}{absPath}{(!string.IsNullOrEmpty(uri.Query) ? $"?{uri.Query}" : string.Empty)}";
            return fullUrl;
        }
    }
}