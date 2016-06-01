// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System.Reflection;
using System.Resources;
using Lunet;

[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("Copyright © 2016 Alexandre Mutel")]

[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion(LunetVersion.AssemblyVersion)]
[assembly: AssemblyFileVersion(LunetVersion.AssemblyVersion)]

[assembly: AssemblyInformationalVersion(LunetVersion.AssemblyVersionInfo)]
[assembly: NeutralResourcesLanguage("en")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly:AssemblyConfiguration("Release")]
#endif

namespace Lunet
{
    /// <summary>
    /// Identifies Lunet version.
    /// </summary>
    /// <remarks>
    /// NOTE: This version number must be in sync with project.json
    /// </remarks>
    class LunetVersion
    {
        /// <summary>
        /// Version without the alpha/beta status.
        /// </summary>
        public const string AssemblyVersion = "0.1.0";

        /// <summary>
        /// The version used for nuget
        /// </summary>
        public const string AssemblyVersionInfo = AssemblyVersion + "-alpha";
    }
}