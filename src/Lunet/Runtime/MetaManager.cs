// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Lunet.Helpers;

namespace Lunet.Runtime
{
    /// <summary>
    /// Manages the meta information associated to a site (from the `_meta` directory and `.lunet` directory)
    /// </summary>
    /// <seealso cref="ManagerBase" />
    public class MetaManager : ManagerBase
    {
        public const string MetaDirectoryName = "_meta";
        private const string PrivateDirectoryName = ".lunet";

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaManager"/> class.
        /// </summary>
        /// <param name="site">The site.</param>
        public MetaManager(SiteObject site) : base(site)
        {
            DirectoryInfo = Site.GetSubDirectory(MetaDirectoryName);
            Directory = DirectoryInfo.FullName;

            PrivateDirectoryInfo = Site.GetSubDirectory(PrivateDirectoryName);
            PrivateDirectory = PrivateDirectoryInfo.FullName;
        }

        public DirectoryInfo DirectoryInfo { get; }

        public string Directory { get; }

        public DirectoryInfo PrivateDirectoryInfo { get; }

        public string PrivateDirectory { get; }

        public IEnumerable<DirectoryInfo> Directories
        {
            get
            {
                yield return DirectoryInfo;

                foreach (var theme in Site.Themes.CurrentList)
                {
                    yield return PathUtil.GetSubDirectory(theme.Directory, MetaDirectoryName);
                }
            }
        }

        public override void InitializeBeforeConfig()
        {
            Site.Generator.CreateDirectory(DirectoryInfo);
            Site.Generator.CreateDirectory(PrivateDirectoryInfo);
        }
    }
}