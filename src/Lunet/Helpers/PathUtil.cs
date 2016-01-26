using System;
using System.IO;
using System.Text;
using Lunet.Runtime;

namespace Lunet.Helpers
{
    internal static class PathUtil
    {
        public static string NormalizePath(string filePath)
        {
            StringBuilder builder = null;
            int index = -1;
            int previousOffset = 0;
            while ((index = filePath.IndexOf('\\', previousOffset)) >= 0)
            {
                if (builder == null)
                {
                    builder = StringBuilderCache.Local();
                }
                int length = index - previousOffset;
                if (length > 0)
                {
                    builder.Append(filePath, previousOffset, length);
                }
                builder.Append('/');
                previousOffset = index + 1;
            }
            if (builder == null)
            {
                return filePath;
            }
            builder.Append(filePath, previousOffset, filePath.Length - previousOffset);
            var str = builder.ToString();
            builder.Length = 0;
            return str;
        }

        public static DirectoryInfo GetSubDirectory(string baseDirectory, string subDirectoryPath)
        {
            if (baseDirectory == null) throw new ArgumentNullException(nameof(baseDirectory));
            if (subDirectoryPath == null) throw new ArgumentNullException(nameof(subDirectoryPath));
            var path = new DirectoryInfo(Path.Combine(baseDirectory, subDirectoryPath));

            // If the sub directory is going above the base directory, log an error
            if (baseDirectory.StartsWith(path.FullName))
            {
                throw new LunetException($"The sub-directory [{subDirectoryPath}] cannot cross above the base directory [{baseDirectory}]");
            }
            return path;
        }

        /// <summary>
        /// Gets a relative path to this site base directory from the specified absolute path.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        /// <param name="fullFilePath">The full file path.</param>
        /// <param name="normalized"><c>true</c> to return a normalize path using only '/' for directory separators</param>
        /// <returns>
        /// A relative path
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="LunetException"></exception>
        public static string GetRelativePath(string rootPath, string fullFilePath, bool normalized = false)
        {
            if (rootPath == null) throw new ArgumentNullException(nameof(rootPath));
            if (fullFilePath == null) throw new ArgumentNullException(nameof(fullFilePath));
            var fullPath = Path.GetFullPath(fullFilePath);
            if (!fullPath.StartsWith(rootPath))
            {
                throw new LunetException($"Cannot query for the relative path [{fullFilePath}] outside the theme directory [{rootPath}]");
            }

            var path = fullPath.Substring(rootPath.Length + 1);
            return normalized ? NormalizePath(path) : path;
        }
    }
}