using System;
using System.IO;
using System.Text;

namespace Lunet.Helpers
{
    internal static class PathUtil
    {
        public static string NormalizePath(string filePath, bool isDirectory)
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
            builder.Length = 0;
            return str;
        }
    }
}