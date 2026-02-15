// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Linq;
using Lunet.Core;
using Lunet.Layouts;
using Lunet.Taxonomies;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;

namespace Lunet.Tests.Taxonomies;

public class TestTaxonomiesModule
{
    [Test]
    public void TestTaxonomyPluginRegistersProcessorAndDefaultDefinitions()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        _ = new TaxonomyPlugin(context.Site, layoutPlugin);

        var processor = context.Site.Content.BeforeProcessingProcessors.Find<TaxonomyProcessor>();
        var taxonomies = context.Site.GetSafeValue<TaxonomyCollection>("taxonomies");

        Assert.NotNull(processor);
        Assert.NotNull(taxonomies);
        Assert.AreEqual("tag", taxonomies!.ScriptObject.GetSafeValue<string>("tags"));
        Assert.AreEqual("category", taxonomies.ScriptObject.GetSafeValue<string>("categories"));
    }

    [Test]
    public void TestTaxonomyProcessorGeneratesTermsAndDynamicPages()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        _ = new TaxonomyPlugin(context.Site, layoutPlugin);
        var processor = context.Site.Content.BeforeProcessingProcessors.Find<TaxonomyProcessor>();

        var firstPage = context.CreateFileContentObject(
            "/docs/first.md",
            """
            +++
            title = "First"
            tags = ["alpha", "beta"]
            categories = ["guides"]
            +++
            First content
            """,
            withFrontMatterScript: true);
        firstPage.Initialize();

        var secondPage = context.CreateFileContentObject(
            "/docs/second.md",
            """
            +++
            title = "Second"
            tags = ["alpha"]
            categories = ["guides"]
            +++
            Second content
            """,
            withFrontMatterScript: true);
        secondPage.Initialize();

        context.Site.Pages.Add(firstPage);
        context.Site.Pages.Add(secondPage);

        processor!.Process(ProcessingStage.BeforeProcessingContent);

        var taxonomies = context.Site.GetSafeValue<TaxonomyCollection>("taxonomies");
        var tags = taxonomies!.ScriptObject.GetSafeValue<Taxonomy>("tags");
        var categories = taxonomies.ScriptObject.GetSafeValue<Taxonomy>("categories");
        var alpha = tags!.Terms.GetSafeValue<TaxonomyTerm>("alpha");
        var guides = categories!.Terms.GetSafeValue<TaxonomyTerm>("guides");

        Assert.NotNull(alpha);
        Assert.AreEqual("/tags/alpha/", alpha!.Url);
        Assert.AreEqual(2, alpha.Pages.Count);
        Assert.NotNull(guides);
        Assert.AreEqual("/categories/guides/", guides!.Url);
        Assert.AreEqual(2, guides.Pages.Count);

        Assert.AreEqual(5, context.Site.DynamicPages.Count);

        var alphaPage = context.Site.DynamicPages.OfType<DynamicContentObject>()
            .First(x => NormalizeUrl(x.Url) == "/tags/alpha");
        Assert.AreEqual("term", alphaPage.LayoutType);
        Assert.AreEqual("tags", alphaPage.Layout);
        Assert.AreSame(alpha, alphaPage.ScriptObjectLocal!.GetSafeValue<TaxonomyTerm>("term"));
    }

    [Test]
    public void TestTaxonomyProcessorSupportsCustomDefinitionsAndMap()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        _ = new TaxonomyPlugin(context.Site, layoutPlugin);
        var processor = context.Site.Content.BeforeProcessingProcessors.Find<TaxonomyProcessor>();
        var taxonomies = context.Site.GetSafeValue<TaxonomyCollection>("taxonomies");

        var map = new ScriptObject();
        map.SetValue("csharp", "c-sharp", true);
        var topics = new ScriptObject();
        topics.SetValue("singular", "topic", true);
        topics.SetValue("url", "/areas/", true);
        topics.SetValue("map", map, true);
        taxonomies!.ScriptObject.SetValue("topics", topics, true);

        var page = context.CreateFileContentObject(
            "/guides/topic.md",
            """
            +++
            title = "Topic page"
            topics = ["csharp"]
            +++
            Topic content
            """,
            withFrontMatterScript: true);
        page.Initialize();
        context.Site.Pages.Add(page);

        processor!.Process(ProcessingStage.BeforeProcessingContent);

        var topicTaxonomy = taxonomies.ScriptObject.GetSafeValue<Taxonomy>("topics");
        var csharpTerm = topicTaxonomy!.Terms.GetSafeValue<TaxonomyTerm>("csharp");

        Assert.NotNull(topicTaxonomy);
        Assert.AreEqual("/areas/", topicTaxonomy!.Url);
        Assert.NotNull(csharpTerm);
        Assert.AreEqual("/areas/c-sharp/", csharpTerm!.Url);
        Assert.IsTrue(context.Site.DynamicPages.Any(x => NormalizeUrl(x.Url) == "/areas"));
        Assert.IsTrue(context.Site.DynamicPages.Any(x => NormalizeUrl(x.Url) == "/areas/c-sharp"));
    }

    [Test]
    public void TestTaxonomyProcessorReportsErrorForInvalidTermsType()
    {
        using var context = new SiteTestContext();
        var layoutPlugin = new LayoutPlugin(context.Site);
        _ = new TaxonomyPlugin(context.Site, layoutPlugin);
        var processor = context.Site.Content.BeforeProcessingProcessors.Find<TaxonomyProcessor>();

        var page = context.CreateFileContentObject(
            "/docs/invalid.md",
            """
            +++
            title = "Invalid"
            tags = "not-an-array"
            +++
            Content
            """,
            withFrontMatterScript: true);
        page.Initialize();
        context.Site.Pages.Add(page);

        processor!.Process(ProcessingStage.BeforeProcessingContent);

        Assert.IsTrue(context.Configuration.LoggerFactory.HasErrors);
    }

    private static string? NormalizeUrl(string? url)
    {
        return url?.TrimEnd('/');
    }
}
