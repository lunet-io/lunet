// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Layouts;

namespace Lunet.Taxonomies
{
    public class TaxonomyManager : ManagerBase
    {
        internal TaxonomyManager(SiteObject site) : base(site)
        {
            List = new List<Taxonomy>();
            Declared = new TaxonomiesObject(this);
            Site.SetValue("taxonomies", Declared, true);
            Site.Plugins.Processors.Add(new TaxonomyProcessor());

            var layoutProcessor = Site.Plugins.Processors.Find<LayoutProcessor>();

            layoutProcessor.RegisterLayoutPathProvider("terms", TermsLayout);
            layoutProcessor.RegisterLayoutPathProvider("term", TermLayout);
        }

        public List<Taxonomy> List { get; }

        public TaxonomiesObject Declared { get; }

        public Taxonomy Find(string name)
        {
            foreach (var tax in List)
            {
                if (tax.Name == name)
                {
                    return tax;
                }
            }
            return null;
        }

        public override void InitializeAfterConfig()
        {
            foreach (var taxonomy in Declared)
            {
                var name = taxonomy.Key;
                var singular = taxonomy.Value as string;
                if (string.IsNullOrWhiteSpace(singular))
                {
                    Site.Error($"Invalid singular form [{singular}] of taxonomy [{name}]. Expecting a string");
                    continue;
                }
                // TODO: verify that plural is a valid identifier

                var tax = Find(name);
                if (tax != null)
                {
                    continue;
                }

                List.Add(new Taxonomy(this, name, singular));
            }

            // Convert taxonomies to readonly after initialization
            Declared.Clear();
            foreach (var taxonomy in List)
            {
                Declared.SetValue(taxonomy.Name, taxonomy, true);
            }
        }

        private static IEnumerable<string> TermsLayout(SiteObject site, string layoutName, string layoutType, string layoutExtension)
        {
            foreach (var metaDir in site.Meta.Directories)
            {
                // try: _meta/layouts/{layoutName}/{layoutType}.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, layoutName, layoutType + layoutExtension);

                // try: _meta/layouts/{layoutName}.{layoutType}.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, layoutName + "." + layoutType + layoutExtension);

                if (layoutName != LayoutProcessor.DefaultLayoutName)
                {
                    // try: _meta/layouts/_default/{layoutType}.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, LayoutProcessor.DefaultLayoutName, layoutType + layoutExtension);

                    // try: _meta/layouts/_default.{layoutType}.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, LayoutProcessor.DefaultLayoutName + "." + layoutType + layoutExtension);
                }
            }
        }

        private static IEnumerable<string> TermLayout(SiteObject site, string layoutName, string layoutType, string layoutExtension)
        {
            foreach (var metaDir in site.Meta.Directories)
            {
                // try: _meta/layouts/{layoutName}/{layoutType}.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, layoutName, layoutType + layoutExtension);

                // try: _meta/layouts/{layoutName}.{layoutType}.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, layoutName + "." + layoutType + layoutExtension);

                // try: _meta/layouts/{layoutName}/list.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, layoutName, LayoutTypes.List + layoutExtension);

                // try: _meta/layouts/{layoutName}.list.{layoutExtension}
                yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, layoutName + "." + LayoutTypes.List + layoutExtension);

                if (layoutName != LayoutProcessor.DefaultLayoutName)
                {
                    // try: _meta/layouts/_default/{layoutType}.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, LayoutProcessor.DefaultLayoutName, layoutType + layoutExtension);

                    // try: _meta/layouts/_default.{layoutType}.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, LayoutProcessor.DefaultLayoutName + "." + layoutType + layoutExtension);

                    // try: _meta/layouts/_default/list.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, LayoutProcessor.DefaultLayoutName, LayoutTypes.List + layoutExtension);

                    // try: _meta/layouts/_default.list.{layoutExtension}
                    yield return Path.Combine(metaDir, LayoutProcessor.LayoutDirectoryName, LayoutProcessor.DefaultLayoutName + "." + LayoutTypes.List + layoutExtension);
                }
            }
        }
    }
}