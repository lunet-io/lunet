using System;
using System.IO;
using System.Text;

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
    }
}