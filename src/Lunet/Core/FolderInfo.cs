// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using Lunet.Helpers;

namespace Lunet.Core
{
    public struct FolderInfo
    {
        public FolderInfo(string fullPath)
        {
            if (fullPath == null) throw new ArgumentNullException(nameof(fullPath));
            Info = new DirectoryInfo(fullPath);
            FullPath = Info.FullName;
        }

        public FolderInfo(DirectoryInfo directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            Info = directory;
            FullPath = directory.FullName;
        }

        public DirectoryInfo Info { get; }

        public string FullPath { get; }

        public bool Exists => Info.Exists;

        public void Create()
        {
            Info.Create();
        }

        public FolderInfo GetSubFolder(string subDirectoryPath)
        {
            if (subDirectoryPath == null) throw new ArgumentNullException(nameof(subDirectoryPath));
            var path = new DirectoryInfo(Path.Combine(this, subDirectoryPath));

            // If the sub directory is going above the base directory, log an error
            if (FullPath.StartsWith(path.FullName))
            {
                throw new LunetException($"The sub-directory [{subDirectoryPath}] cannot cross above the base directory [{FullPath}]");
            }
            return path;
        }

        /// <summary>
        /// Gets a relative path to this site base directory from the specified absolute path.
        /// </summary>
        /// <param name="fullFilePath">The full file path.</param>
        /// <param name="flags"></param>
        /// <returns>
        /// A relative path
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="LunetException"></exception>
        public string GetRelativePath(string fullFilePath, PathFlags flags)
        {
            if (fullFilePath == null) throw new ArgumentNullException(nameof(fullFilePath));
            var fullPath = Path.GetFullPath(fullFilePath);
            if (!fullPath.StartsWith(FullPath))
            {
                throw new LunetException($"Cannot query for the relative path [{fullFilePath}] outside the theme directory [{FullPath}]");
            }

            var path = fullPath.Substring(FullPath.Length + 1);
            return flags.Normalize() ? PathUtil.NormalizePath(path, flags.IsDirectory()) : path;
        }

        public static implicit operator FolderInfo(string folderPath)
        {
            return new FolderInfo(folderPath);
        }

        public static implicit operator FolderInfo(DirectoryInfo directory)
        {
            return new FolderInfo(directory);
        }

        public static implicit operator string(FolderInfo folderInfo)
        {
            return folderInfo.FullPath;
        }

        public static implicit operator DirectoryInfo(FolderInfo folderInfo)
        {
            return folderInfo.Info;
        }

        public override string ToString()
        {
            return FullPath;
        }
    }
}