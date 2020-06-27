// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lunet.Core;
using Lunet.Layouts;
using Scriban.Runtime;
using Zio;

namespace Lunet.Taxonomies
{
    public class TaxonomyProcessor : ProcessorBase<TaxonomyPlugin>
    {
        public override string Name => "taxonomies";

        public TaxonomyProcessor(TaxonomyPlugin plugin, LayoutPlugin layoutPlugin) : base(plugin)
        {
            if (layoutPlugin == null) throw new ArgumentNullException(nameof(layoutPlugin));
            List = new TaxonomyCollection();

            Site.SetValue("taxonomies", List, true);

            layoutPlugin.Processor.RegisterLayoutPathProvider("terms", TermsLayout);
            layoutPlugin.Processor.RegisterLayoutPathProvider("term", TermPagesLayout);
        }

        public TaxonomyCollection List { get; }

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

        public override void Process(ProcessingStage stage)
        {
            Debug.Assert(stage == ProcessingStage.BeforeProcessingContent);

            foreach (var taxonomy in List.ScriptObject)
            {
                var name = taxonomy.Key;
                var singular = taxonomy.Value as string;
                if (string.IsNullOrWhiteSpace(singular))
                {
                    // Don't log an error, as we just want to 
                    // Site.Error($"Invalid singular form [{singular}] of taxonomy [{name}]. Expecting a string");
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
            List.ScriptObject.Clear();
            foreach (var taxonomy in List)
            {
                List.ScriptObject.SetValue(taxonomy.Name, taxonomy, true);
            }

            foreach (var page in Site.Pages)
            {
                var dyn = (DynamicObject)page;
                foreach (var tax in List)
                {
                    var termsObj = dyn[tax.Name];
                    var terms = termsObj as ScriptArray;
                    if (termsObj == null)
                    {
                        continue;
                    }
                    if (terms == null)
                    {
                        Site.Error("Invalid type");
                        continue;
                    }

                    foreach (var termNameObj in terms)
                    {
                        var termName = termNameObj as string;
                        if (termName == null)
                        {
                            Site.Error("// TODO ERROR ON TERM");
                            continue;
                        }

                        object termObj;
                        TaxonomyTerm term;
                        if (!tax.Terms.TryGetValue(termName, out termObj))
                        {
                            termObj = term = new TaxonomyTerm(tax, termName);
                            tax.Terms[termName] = termObj;
                        }
                        else
                        {
                            term = (TaxonomyTerm)termObj;
                        }

                        term.Pages.Add(page);
                    }
                }
            }

            // Update taxonomy computed
            foreach (var tax in List)
            {
                tax.Update();
            }

            // Generate taxonomy pages
            foreach (var tax in List)
            {
                // Generate a term page for each term in the current taxonomy
                foreach (var term in tax.Terms.Values.OfType<TaxonomyTerm>())
                {
                    // term.Url
                    var content = new ContentObject(Site, tax.Name)
                    {
                        ScriptObjectLocal = new DynamicObject<TaxonomyTerm>(term),
                        Url = term.Url,
                        Layout = tax.Name,
                        LayoutType = "term",
                        ContentType = ContentType.Html
                    };

                    content.ScriptObjectLocal.SetValue("pages", term.Pages, true);
                    content.ScriptObjectLocal.SetValue("taxonomy", tax, true);
                    content.ScriptObjectLocal.SetValue("term", term, true);

                    foreach (var page in term.Pages)
                    {
                        content.Dependencies.Add(new PageContentDependency(page));
                    }

                    Site.DynamicPages.Add(content);
                }

                // Generate a terms page for the current taxonomy
                {
                    var content = new ContentObject(Site, tax.Name)
                    {
                        ScriptObjectLocal = new DynamicObject<Taxonomy>(tax),
                        Url = tax.Url,
                        Layout = tax.Name,
                        LayoutType = "terms",
                        ContentType = ContentType.Html
                    };
                    content.ScriptObjectLocal.SetValue("taxonomy", tax, true);

                    // TODO: Add dependencies

                    Site.DynamicPages.Add(content);
                }
            }
        }

        private static IEnumerable<UPath> TermsLayout(SiteObject site, string layoutName, string layoutType)
        {
            // try: _meta/layouts/{layoutName}/terms.{layoutExtension}
            yield return (UPath)layoutName / (layoutType);

            // try: _meta/layouts/{layoutName}.terms.{layoutExtension}
            yield return (UPath)(layoutName + "." + layoutType);

            if (layoutName != LayoutProcessor.DefaultLayoutName)
            {
                // try: _meta/layouts/_default/terms.{layoutExtension}
                yield return (UPath)LayoutProcessor.DefaultLayoutName / (layoutType);

                // try: _meta/layouts/_default.terms.{layoutExtension}
                yield return (UPath)(LayoutProcessor.DefaultLayoutName + "." + layoutType);
            }
        }

        private static IEnumerable<UPath> TermPagesLayout(SiteObject site, string layoutName, string layoutType)
        {
            // try: _meta/layouts/{layoutName}/{layoutType}.{layoutExtension}
            yield return (UPath)layoutName / (layoutType);

            // try: _meta/layouts/{layoutName}.{layoutType}.{layoutExtension}
            yield return (UPath)(layoutName + "." + layoutType);

            // try: _meta/layouts/{layoutName}/list.{layoutExtension}
            yield return (UPath)layoutName / (LayoutTypes.List);

            // try: _meta/layouts/{layoutName}.list.{layoutExtension}
            yield return (UPath)(layoutName + "." + LayoutTypes.List);

            if (layoutName != LayoutProcessor.DefaultLayoutName)
            {
                // try: _meta/layouts/_default/{layoutType}.{layoutExtension}
                yield return (UPath)LayoutProcessor.DefaultLayoutName / (layoutType);

                // try: _meta/layouts/_default.{layoutType}.{layoutExtension}
                yield return (UPath)(LayoutProcessor.DefaultLayoutName + "." + layoutType);

                // try: _meta/layouts/_default/list.{layoutExtension}
                yield return (UPath)LayoutProcessor.DefaultLayoutName / (LayoutTypes.List);

                // try: _meta/layouts/_default.list.{layoutExtension}
                yield return (UPath)(LayoutProcessor.DefaultLayoutName + "." + LayoutTypes.List);
            }
        }
    }
}