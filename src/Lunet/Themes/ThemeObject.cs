// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using System.IO;
using Lunet.Helpers;
using Lunet.Runtime;

namespace Lunet.Themes
{
    public sealed class ThemeObject : LunetObject
    {
        internal ThemeObject(SiteObject site, ThemeDescription desc, string directory)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            Site = site;
            Name = desc.Name;
            Description = desc.Description;
            Url = desc.Url;
            DirectoryInfo = new DirectoryInfo(directory);
            Directory = DirectoryInfo.FullName;
            Path = site.GetRelativePath(directory, true);

            DynamicObject.SetValue("name", Name, true);
            DynamicObject.SetValue("description", Description, true);
            DynamicObject.SetValue("url", Url, true);
            DynamicObject.SetValue("path", Path, true);
        }
        public SiteObject Site { get; }

        public DirectoryInfo DirectoryInfo { get; }

        public string Directory { get; }

        public string Name { get; }

        public string Description { get; }

        public string Url { get; }

        public string Path { get; }

        /// <summary>
        /// Gets a relative path to this site base directory from the specified absolute path.
        /// </summary>
        /// <param name="fullFilePath">The full file path.</param>
        /// <param name="normalized"><c>true</c> to return a normalize path using only '/' for directory separators</param>
        /// <returns>A relative path</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="LunetException"></exception>
        public string GetRelativePath(string fullFilePath, bool normalized = false)
        {
            return PathUtil.GetRelativePath(Directory, fullFilePath, normalized);
        }
    }
}