// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Bundles;
using Lunet.Menus;

namespace Lunet.Api.DotNet;

public class ApiDotNetModule : SiteModule<ApiDotNetPlugin>
{
}
    
public class ApiDotNetPlugin : SitePlugin
{
    private ApiDotNetConfig? _apiDotNetConfig;
    private bool _uiBundleInjected;

    public ApiDotNetPlugin(SiteObject site, ApiPlugin api, BundlePlugin? bundles = null, MenuPlugin? menus = null) : base(site)
    {
        Api = api;
        Bundles = bundles;
        Menus = menus;
        Api.Register("dotnet", GetDotNetObject);
    }

    public ApiPlugin Api { get; }

    public BundlePlugin? Bundles { get; }

    public MenuPlugin? Menus { get; }

    private ApiDotNetConfig GetDotNetObject()
    {
        if (_apiDotNetConfig == null)
        {
            EnsureApiDotNetUiBundled();

            _apiDotNetConfig = new ApiDotNetConfig();
            var processor = new ApiDotNetProcessor(this, _apiDotNetConfig);
            Site.Content.BeforeLoadingProcessors.Add(processor);
        }
        return _apiDotNetConfig;
    }

    private void EnsureApiDotNetUiBundled()
    {
        if (_uiBundleInjected || Bundles is null)
        {
            return;
        }

        var defaultBundle = Bundles.GetOrCreateBundle(null);
        defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/api/dotnet/lunet-api-dotnet-ui.js", mode: "defer");
        _uiBundleInjected = true;
    }
}
