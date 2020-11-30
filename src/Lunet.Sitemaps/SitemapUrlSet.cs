// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Lunet.Sitemaps
{
    [XmlType("urlset")]
    [XmlRoot(Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
    public class SitemapUrlSet
    {
        public SitemapUrlSet()
        {
            Urls = new List<SitemapUrl>();
        }

        [XmlElement("url")]
        public List<SitemapUrl> Urls
        {
            get;
            set;
        }
    }
}