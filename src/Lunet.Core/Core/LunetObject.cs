// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
namespace Lunet.Core
{
    /// <summary>
    /// Base class for an lunet object that provides a dynamic object
    /// accessible from scripts.
    /// </summary>
    public class LunetObject : DynamicObject<SiteObject>
    {
        public LunetObject(SiteObject site) : base(site)
        {
            Version = LunetVersion.AssemblyVersionInfo;
            SetValue("version", Version, true);
            Environment = "Development";
        }

        public string Version { get; }

        public string Environment
        {
            get { return GetSafeValue<string>("env"); }
            set { this["env"] = value; }
        }
    }
}