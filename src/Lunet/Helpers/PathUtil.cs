using System;
using System.IO;
using System.Text;

namespace Lunet.Helpers
{
    internal static class PathUtil
    {
        private static readonly char[] TrimCharStart = new[] {'/'};

        public static string NormalizeRelativePath(string filePath, bool isDirectory)
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
                if (filePath.StartsWith("/"))
                {
                    filePath = filePath.TrimStart(TrimCharStart);
                }

                // Append '/' if it is a directory
                if (isDirectory && !string.IsNullOrWhiteSpace(filePath) && !filePath.EndsWith("/"))
                {
                    builder = StringBuilderCache.Local();
                    builder.Append(filePath);
                    builder.Append('/');
                    return builder.ToString();
                }
                return filePath;
            }
            builder.Append(filePath, previousOffset, filePath.Length - previousOffset);

            // Append '/' if it is a directory
            if (isDirectory && builder.Length > 0 && builder[builder.Length - 1] != '/')
            {
                builder.Append('/');
            }

            var str = builder.ToString();

            if (str.StartsWith("/"))
            {
                str = filePath.TrimStart(TrimCharStart);
            }
            builder.Length = 0;
            return str;
        }
    }
}