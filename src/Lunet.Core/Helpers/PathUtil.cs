using System;
using System.IO;
using System.Text;

namespace Lunet.Helpers
{
    internal static class PathUtil
    {
        private static readonly char[] TrimCharStart = new[] {'/'};


        public static string NormalizeExtension(string extension)
        {
            return extension.StartsWith(".") ? extension : "." + extension;
        }

        public static string Normalize(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            return new FileInfo(filePath).FullName;
        }

        public static T Normalize<T>(this T fileInfo) where T : FileSystemInfo
        {
            if (fileInfo is FileInfo)
            {
                return (T)(object)new FileInfo(fileInfo.FullName);
            }
            return (T)(object)new DirectoryInfo(fileInfo.FullName);
        }

        public static string NormalizeUrl(string filePath, bool isDirectory)
        {
            return NormalizePathOrUrl(filePath, isDirectory, true);
        }

        public static string NormalizeRelativePath(string filePath, bool isDirectory)
        {
            return NormalizePathOrUrl(filePath, isDirectory, false);
        }

        private static string NormalizePathOrUrl(string filePath, bool isDirectory, bool isUrl)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            filePath = filePath.Trim();

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
                if (index > 0)
                {
                    builder.Append('/');
                }
                previousOffset = index + 1;
            }
            if (builder == null)
            {
                // Remove leading /
                var startsBySlash2 = filePath.StartsWith("/");
                if (!isUrl && startsBySlash2)
                {
                    filePath = filePath.TrimStart(TrimCharStart);
                }
                else if (isUrl && !startsBySlash2)
                {
                    builder = StringBuilderCache.Local();
                    builder.Append("/");
                    builder.Append(filePath);
                }

                // Append '/' if it is a directory
                if (isDirectory && !string.IsNullOrWhiteSpace(filePath) && !filePath.EndsWith("/"))
                {
                    if (builder == null)
                    {
                        builder = StringBuilderCache.Local();
                        builder.Append(filePath);
                    }
                    builder.Append('/');
                }

                return builder?.ToString() ?? filePath;
            }
            builder.Append(filePath, previousOffset, filePath.Length - previousOffset);

            // Append '/' if it is a directory
            if (isDirectory && builder.Length > 0 && builder[builder.Length - 1] != '/')
            {
                builder.Append('/');
            }

            var str = builder.ToString();

            var startsBySlash = str.StartsWith("/");
            if (!isUrl && startsBySlash)
            {
                str = str.TrimStart(TrimCharStart);
            } else if (isUrl && !startsBySlash)
            {
                str = "/" + str;
            }

            builder.Length = 0;
            return str;
        }
    }
}