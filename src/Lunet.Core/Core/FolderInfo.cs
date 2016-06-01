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
        public static readonly FolderInfo Current = new FolderInfo(".");

        public FolderInfo(string fullPath)
        {
            fullPath = fullPath ?? ".";
            FullName = new DirectoryInfo(fullPath).FullName;
            Info = new DirectoryInfo(FullName);
        }

        public FolderInfo(DirectoryInfo directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            FullName = directory.FullName;
            Info = new DirectoryInfo(FullName);
        }

        public DirectoryInfo Info { get; }

        public string FullName { get; }

        public bool Exists => Info.Exists;

        public void Create()
        {
            Info.Create();
        }

        public string Combine(string relativePath)
        {
            var newPath = PathUtil.NormalizeRelativePath(relativePath, false);
            return new FileInfo(Path.Combine(this, newPath)).FullName;
        }
        public FileInfo CombineToFile(string relativePath)
        {
            var newPath = PathUtil.NormalizeRelativePath(relativePath, false);
            return new FileInfo(Path.Combine(this, newPath)).Normalize();
        }

        public FolderInfo GetSubFolder(string subDirectoryPath)
        {
            if (subDirectoryPath == null) throw new ArgumentNullException(nameof(subDirectoryPath));
            var path = new DirectoryInfo(Combine(subDirectoryPath));

            // If the sub directory is going above the base directory, log an error
            if (FullName.StartsWith(path.FullName))
            {
                throw new LunetException($"The sub-directory [{subDirectoryPath}] cannot cross above the base directory [{FullName}]");
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
            if (!fullPath.StartsWith(FullName))
            {
                throw new LunetException($"Cannot query for the relative path [{fullFilePath}] outside the theme directory [{FullName}]");
            }

            var path = fullPath.Substring(FullName.Length + 1);
            return flags.Normalize() ? PathUtil.NormalizeRelativePath(path, flags.IsDirectory()) : path;
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
            return folderInfo.FullName;
        }

        public static implicit operator DirectoryInfo(FolderInfo folderInfo)
        {
            return folderInfo.Info;
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}