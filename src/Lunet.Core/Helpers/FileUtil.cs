// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.IO;
using Lunet.Core;

namespace Lunet.Helpers
{
    internal static class FileUtil
    {
        public static void DirectoryCopy(FolderInfo sourcedir, FolderInfo destDir, bool copySubDirs, bool overwrite)
        {
            // code from https://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx

            if (!sourcedir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourcedir.FullName);
            }

            DirectoryInfo[] dirs = sourcedir.Info.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!destDir.Exists)
            {
                destDir.Create();
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourcedir.Info.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDir, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    var temppath = Path.Combine(destDir, subdir.Name);
                    DirectoryCopy(subdir, temppath, true, overwrite);
                }
            }
        }
    }
}