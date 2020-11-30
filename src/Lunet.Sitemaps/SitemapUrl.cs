// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace Lunet.Sitemaps
{
    public class SitemapUrl
    {
        public SitemapUrl()
        {
        }


        public SitemapUrl(string url)
        {
            Url = url;
        }

        [XmlElement("loc")]
        public string Url { get; set; }

        [XmlElement("lastmod")]
        public string LastModifiedAsText
        {
            get => LastModified?.ToString("yyyy-MM-dd");
            set => throw new NotSupportedException("LastModifiedAsText Not supporting serialization");
        }

        [XmlIgnore]
        public DateTime? LastModified { get; set; }

        public bool ShouldSerializeLastModifiedAsText()
        {
            return LastModified.HasValue;
        }
        
        [XmlElement("changefreq")]
        public string ChangeFrequency { get; set; }

        public bool ShouldSerializeChangeFrequency()
        {
            return ChangeFrequency != null;
        }

        [XmlElement("priority")]
        public float? Priority { get; set; }

        public bool ShouldSerializePriority()
        {
            return Priority.HasValue;
        }
    }
}