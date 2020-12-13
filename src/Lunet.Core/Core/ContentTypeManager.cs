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
        private readonly Dictionary<string, ContentType> _extensionToContentType;
        private readonly HashSet<ContentType> _htmlContentType;

        public ContentTypeManager()
        {
            _extensionToContentType = new Dictionary<string, ContentType>(StringComparer.OrdinalIgnoreCase);
            _htmlContentType = new HashSet<ContentType>();
            AddBuiltins();
        }

        public void AddContentType(string extension, ContentType contentType)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            extension = PathUtil.NormalizeExtension(extension);
            _extensionToContentType[extension] = contentType;
        }

        public bool IsHtmlContentType(ContentType contentType)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            return _htmlContentType.Contains(contentType);
        }

        public void RegisterHtmlContentType(ContentType contentType)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            _htmlContentType.Add(contentType);
        }

        public ContentType GetContentType(string extension)
        {
            if (extension == null) return ContentType.Empty;

            extension = PathUtil.NormalizeExtension(extension);
            ContentType contentType;
            return _extensionToContentType.TryGetValue(extension, out contentType)
                ? contentType
                : new ContentType(extension.TrimStart(new[] {'.'}));
        }

        public HashSet<string> GetExtensionsByContentType(ContentType type)
        {
            var exts = new HashSet<string>();
            foreach (var extAndType in _extensionToContentType)
            {
                if (extAndType.Value == type)
                {
                    exts.Add(extAndType.Key);
                }
            }

            // If we haven't found any extensions, add at least one from the type
            if (exts.Count == 0)
            {
                exts.Add($".{type.Name}");
            }

            return exts;
        }

        private void AddBuiltins()
        {
            _extensionToContentType[".htm"] = ContentType.Html;
            _extensionToContentType[".html"] = ContentType.Html;
            _extensionToContentType[".scriban-html"] = ContentType.Html;
            _extensionToContentType[".scriban-htm"] = ContentType.Html;
            _extensionToContentType[".sbn-html"] = ContentType.Html;
            _extensionToContentType[".sbn-htm"] = ContentType.Html;
            _extensionToContentType[".sbnhtml"] = ContentType.Html;
            _extensionToContentType[".sbnhtm"] = ContentType.Html;
            _extensionToContentType[".markdown"] = ContentType.Markdown;
            _extensionToContentType[".md"] = ContentType.Markdown;
            // Not used, but for example
            _extensionToContentType[".jpg"] = ContentType.Jpeg;
            _extensionToContentType[".jpeg"] = ContentType.Jpeg;

            _htmlContentType.Add(ContentType.Html);
            _htmlContentType.Add(ContentType.Markdown);
        }
    }
}