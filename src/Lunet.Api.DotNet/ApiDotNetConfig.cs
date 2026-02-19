// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Scriban.Runtime;

namespace Lunet.Api.DotNet;

public class ApiDotNetConfig : ApiConfig
{
    public ApiDotNetConfig()
    {
        SolutionConfiguration = "Release";
    }

    public string? Title
    {
        get => this.GetSafeValue<string>("title");
        set => this.SetValue("title", value);
    }

    public string SolutionConfiguration
    {
        get => this.GetSafeValue<string>("config") ?? "Release";
        set => this.SetValue("config", value);
    }
        
    public ScriptArray? Projects
    {
        get => this.GetSafeValue<ScriptArray>("projects");
        set => this.SetValue("projects", value);
    }

    public ScriptObject? Properties
    {
        get => this.GetSafeValue<ScriptObject>("properties");
        set => this.SetValue("properties", value);
    }

    public ScriptArray? References
    {
        get => this.GetSafeValue<ScriptArray>("references");
        set => this.SetValue("references", value);
    }

    public string? IncludeHelper
    {
        get => this.GetSafeValue<string>("include_helper");
        set => this.SetValue("include_helper", value);
    }

    public string? Layout
    {
        get => this.GetSafeValue<string>("layout");
        set => this.SetValue("layout", value);
    }

    public string BasePath
    {
        get => this.GetSafeValue<string>("path") ?? "/api";
        set => this.SetValue("path", value);
    }

    public string MenuName
    {
        get => this.GetSafeValue<string>("menu_name") ?? "api";
        set => this.SetValue("menu_name", value);
    }

    public string? MenuTitle
    {
        get => this.GetSafeValue<string>("menu_title");
        set => this.SetValue("menu_title", value);
    }

    public int MenuWidth
    {
        get
        {
            var width = this.GetSafeValue<int>("menu_width");
            return width == 0 ? 4 : width;
        }
        set => this.SetValue("menu_width", value);
    }

    public int MaxSlugLength
    {
        get
        {
            var length = this.GetSafeValue<int>("max_slug_length");
            return ApiDotNetSlugGenerator.NormalizeMaxLength(length);
        }
        set => this.SetValue("max_slug_length", value);
    }
}
