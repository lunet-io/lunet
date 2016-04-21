// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Core;
using Lunet.Helpers;
using Scriban.Runtime;

namespace Lunet.Bundles
{
    public class BundleObject : DynamicObject<BundleManager>
    {
        private delegate void StringFunctionDelegate(string filepath);

        public BundleObject(BundleManager manager, string name) : base(manager)
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

            Import(BundleObjectProperties.JsType, (StringFunctionDelegate)FunctionJs);
            Import(BundleObjectProperties.CssType, (StringFunctionDelegate)FunctionCss);
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

        public void AddLink(string type, string url)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (url == null) throw new ArgumentNullException(nameof(url));
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
            {
                throw new LunetException($"Invalid url [{url}]");
            }

            var link = new BundleLink(this, type, uri.IsAbsoluteUri ? null : uri.ToString(), uri.IsAbsoluteUri ? uri.ToString() : null);
            Links.Add(link);
        }

        private void FunctionJs(string fileArg)
        {
            if (fileArg == null) throw new ArgumentNullException(nameof(fileArg));
            AddLink(BundleObjectProperties.JsType, fileArg);
        }

        private void FunctionCss(string fileArg)
        {
            if (fileArg == null) throw new ArgumentNullException(nameof(fileArg));
            AddLink(BundleObjectProperties.CssType, fileArg);
        }
    }
}