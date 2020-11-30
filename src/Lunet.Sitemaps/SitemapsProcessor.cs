// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Lunet.Core;

namespace Lunet.Sitemaps
{
    public class SitemapsProcessor : ContentProcessor<SitemapsPlugin>
    {
        private SitemapUrlSet _urlSet;

        public SitemapsProcessor(SitemapsPlugin plugin) : base(plugin)
        {

        }

        public override void Process(ProcessingStage stage)
        {
            if (!Plugin.Enable) return;

            if (stage == ProcessingStage.BeforeLoadingContent)
            {
                _urlSet = new SitemapUrlSet();
            }
            else if (stage == ProcessingStage.BeforeProcessingContent)
            {
                const string ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                var xmlSerializer = new XmlSerializer(typeof(SitemapUrlSet), ns);
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", ns);

                var sitemap = new Utf8StringWriter();
                xmlSerializer.Serialize(sitemap, _urlSet, namespaces);

                // Generate sitemap.xml
                var content = new ContentObject(Site)
                {
                    Url = "/sitemap.xml",
                    ContentType = ContentType.Xml,
                    Content = sitemap.ToString()
                };
                Site.DynamicPages.Add(content);

                // Generate robots.txt
                var robotsContent = new ContentObject(Site)
                {
                    Url = "/robots.txt",
                    ContentType = ContentType.Txt,
                    Content = $"Sitemap: {Site.Helpers.Urlsite(content.Url)}"
                };
                Site.DynamicPages.Add(robotsContent);
            }
        }

        public override ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage)
        {
            var contentType = file.ContentType;
            if (!Plugin.Enable || !(contentType == ContentType.Markdown || contentType == ContentType.Html))
            {
                return ContentResult.Continue;
            }

            // if sitemap is false, then don't index it
            var allowSiteMap = file["sitemap"] is bool v ? v : true;
            if (!allowSiteMap) return ContentResult.Continue;
            
            var url = Site.Helpers.Urlsite(file.Url);

            var sitemapUrl = new SitemapUrl(url)
            {
                LastModified = file.ModifiedTime.Ticks == 0 ? DateTime.Now : file.ModifiedTime
            };

            _urlSet.Urls.Add(sitemapUrl);

            return ContentResult.Continue;
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}