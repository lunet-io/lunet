using System;
using System.Globalization;
using System.Text;
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
            SetValue("urlize", DelegateCustomFunction.CreateFunc((Func<string, string>)Urlize), true);
        }

        public object Head
        {
            get => GetSafeValue<object>("Head");
            set => SetValue("Head", value);
        }

        public string Urlize(string url)
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