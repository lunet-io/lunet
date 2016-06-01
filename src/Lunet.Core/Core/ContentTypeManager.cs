// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Lunet.Helpers;

namespace Lunet.Core
{
    public class ContentTypeManager
    {
        private readonly Dictionary<string, ContentType> extensionToContentType;
        private readonly HashSet<ContentType> htmlContentType;

        public ContentTypeManager()
        {
            extensionToContentType = new Dictionary<string, ContentType>(StringComparer.OrdinalIgnoreCase);
            htmlContentType = new HashSet<ContentType>();
            AddBuiltins();
        }

        public void AddContentType(string extension, ContentType contentType)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            extension = PathUtil.NormalizeExtension(extension);
            extensionToContentType[extension] = contentType;
        }

        public bool IsHtmlContentType(ContentType contentType)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            return htmlContentType.Contains(contentType);
        }

        public void RegisterHtmlContentType(ContentType contentType)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            htmlContentType.Add(contentType);
        }

        public ContentType GetContentType(string extension)
        {
            if (extension == null) return ContentType.Empty;

            extension = PathUtil.NormalizeExtension(extension);
            ContentType contentType;
            return extensionToContentType.TryGetValue(extension, out contentType)
                ? contentType
                : new ContentType(extension.TrimStart(new[] {'.'}));
        }

        private void AddBuiltins()
        {
            extensionToContentType[".htm"] = ContentType.Html;
            extensionToContentType[".html"] = ContentType.Html;
            extensionToContentType[".markdown"] = ContentType.Markdown;
            extensionToContentType[".md"] = ContentType.Markdown;
            // Not used, but for example
            extensionToContentType[".jpg"] = ContentType.Jpeg;
            extensionToContentType[".jpeg"] = ContentType.Jpeg;

            htmlContentType.Add(ContentType.Html);
            htmlContentType.Add(ContentType.Markdown);
        }
    }
}