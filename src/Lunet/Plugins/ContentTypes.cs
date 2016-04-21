// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Helpers;

namespace Lunet.Plugins
{
    public class ContentTypes
    {
        private readonly Dictionary<string, string> extensionToContentType;

        public const string Html = "html";

        public const string Markdown = "md";

        public ContentTypes()
        {
            extensionToContentType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".htm"] = Html,
                [".markdown"] = Markdown,
                [".jpeg"] = "jpg",
            };
        }

        public void AddContentType(string extension, string contentType)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            extension = PathUtil.NormalizeExtension(extension);
            extensionToContentType[extension] = contentType;
        }

        public string GetContentType(string extension)
        {
            if (extension == null) return null;

            extension = PathUtil.NormalizeExtension(extension);
            string contentType;
            if (extensionToContentType.TryGetValue(extension, out contentType))
            {
                return contentType;
            }

            return extension.TrimStart(new [] {'.'});
        }
    }
}