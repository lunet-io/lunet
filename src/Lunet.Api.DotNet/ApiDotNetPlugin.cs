// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Menus;

namespace Lunet.Api.DotNet;

public class ApiDotNetModule : SiteModule<ApiDotNetPlugin>
{
}
    
public class ApiDotNetPlugin : SitePlugin
{
    private ApiDotNetConfig? _apiDotNetConfig;

    public ApiDotNetPlugin(SiteObject site, ApiPlugin api, MenuPlugin? menus = null) : base(site)
    {
        Api = api;
        Menus = menus;
        Api.Register("dotnet", GetDotNetObject);
    }

    public ApiPlugin Api { get; }

    public MenuPlugin? Menus { get; }

    private ApiDotNetConfig GetDotNetObject()
    {
        if (_apiDotNetConfig == null)
        {
            _apiDotNetConfig = new ApiDotNetConfig();
            var processor = new ApiDotNetProcessor(this, _apiDotNetConfig);
            Site.Content.BeforeLoadingProcessors.Add(processor);
        }
        return _apiDotNetConfig;
    }
}
