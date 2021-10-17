// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;

namespace Lunet.Core
{
    public readonly struct ContentType : IEquatable<ContentType>
    {
        public static ContentType Empty = new ContentType(string.Empty);

        public static readonly ContentType Html = new ContentType("html");

        public static readonly ContentType Markdown = new ContentType("md");

        public static readonly ContentType Jpeg = new ContentType("jpg");

        public static readonly ContentType Css = new ContentType("css");

        public static readonly ContentType Js = new ContentType("js");

        public static readonly ContentType Xml = new ContentType("xml");

        public static readonly ContentType Txt = new ContentType("txt");
        
        public bool IsHtmlLike()
        {
            return this.Equals(Html) || this.Equals(Markdown);
        }
        
        public ContentType(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Name = name;
        }

        public string Name { get;  }

        public bool Equals(ContentType other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ContentType contentType && Equals(contentType);
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        public static bool operator ==(ContentType left, ContentType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentType left, ContentType right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}