using System;
using System.Globalization;
using System.Text;
using Lunet.Scripts;
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
            SetValue("urlsite", DelegateCustomFunction.CreateFunc((Func<string, string>)Urlsite), true);

            // Helpers used for declaring panels (e.g {{NOTE do}}This is a note.{{end}}
            var helpers = @"
# Defines the generic panel helper function
func PANEL
    `<div class='` + $0 + `'>`
        `<div class='` + $0 + `-heading'>`
            `<i class='` + $0 + `-icon'></i><span class='` + $0 + `-heading-text'></span>`
        `</div>`
        `<div class='` + $0 + `-content'>` + '\n\n'
            $1
        `</div>`
    '</div>\n\n'
end

# Defines panel functions
func NOTE; PANEL 'lunet-block-note' @$0; end
func TIP; PANEL 'lunet-block-tip' @$0; end
func WARNING; PANEL 'lunet-block-warning' @$0; end
func IMPORTANT; PANEL 'lunet-block-important' @$0; end
func CAUTION; PANEL 'lunet-block-caution' @$0; end
func CALLOUT; PANEL 'lunet-block-callout' @$0; end
";
            
            parent.Scripts.TryImportScript(helpers, "internal_helpers", this, ScriptFlags.AllowSiteFunctions, out _);
        }

        public object Head
        {
            get => GetSafeValue<object>("Head");
            set => SetValue("Head", value);
        }

        public string Urlsite(string url)
        {
            if (url == null)
            {
                url = "/";
            }

            if (!url.StartsWith("/"))
            {
                throw new ArgumentException($"Invalid url `{url}`. Expecting an absolute url starting with /", nameof(url));
            }
            
            if (!UPath.TryParse(url, out var urlPath))
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