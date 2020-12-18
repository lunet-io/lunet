// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Scriban.Runtime;

namespace Lunet.Api.DotNet
{
    public class ApiDotNetConfig : ApiConfig
    {
        public ApiDotNetConfig()
        {
            SolutionConfiguration = "Release";
        }

        public string SolutionConfiguration
        {
            get => this.GetSafeValue<string>("config");
            set => this.SetValue("config", value);
        }
        
        public ScriptArray Projects
        {
            get => this.GetSafeValue<ScriptArray>("projects");
            set => this.SetValue("projects", value);
        }

        public ScriptObject Properties
        {
            get => this.GetSafeValue<ScriptObject>("properties");
            set => this.SetValue("properties", value);
        }
    }
}