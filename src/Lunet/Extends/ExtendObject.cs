// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;

namespace Lunet.Extends
{
    public sealed class ExtendObject : LunetObject
    {
        internal ExtendObject(SiteObject site, string fullName, string name, string version, string description, string url, string directory)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (fullName == null) throw new ArgumentNullException(nameof(fullName));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (directory == null) throw new ArgumentNullException(nameof(directory));

            Site = site;
            FullName = fullName;
            Name = name;
            Version = version;
            Description = description;
            Url = url;
            Directory = directory;
            Path = site.GetRelativePath(directory, PathFlags.Directory|PathFlags.Normalize);

            SetValue("name", Name, true);
            SetValue("version", Version, true);
            SetValue("description", Description, true);
            SetValue("url", Url, true);
            SetValue("path", Path, true);
        }
        public SiteObject Site { get; }

        public FolderInfo Directory { get; }

        public string Name { get; }

        public string FullName { get; }

        public string Version { get; }

        public string Description { get; }

        public string Url { get; }

        public string Path { get; }

        /// <summary>
        /// Gets a relative path to this site base directory from the specified absolute path.
        /// </summary>
        /// <param name="fullFilePath">The full file path.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>
        /// A relative path
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="LunetException"></exception>
        public string GetRelativePath(string fullFilePath, PathFlags flags)
        {
            return Directory.GetRelativePath(fullFilePath, flags);
        }
    }
}