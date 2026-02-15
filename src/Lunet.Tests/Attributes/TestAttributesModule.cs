// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Reflection;
using Lunet.Attributes;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;
using Zio;

namespace Lunet.Tests.Attributes;

public class TestAttributesModule
{
    [Test]
    public void TestAttributesObjectHasDefaultBlogPostRule()
    {
        var attributes = new AttributesObject();

        Assert.AreEqual(1, attributes.Count);
        var defaultRule = attributes[0];
        Assert.IsTrue(defaultRule.Match);
        Assert.AreEqual("/**/[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]*.*", defaultRule.Pattern);
        Assert.NotNull(defaultRule.Setters);
        Assert.AreEqual("/:section/:year/:month/:day/:slug:output_ext", defaultRule.Setters!["url"]);
    }

    [Test]
    public void TestAttributesMatchAndUnmatchRegisterRules()
    {
        var attributes = new AttributesObject();

        var matchRule = attributes.Match("/blog/**/*.md", new ScriptObject { { "layout", "blog" } });
        var unmatchRule = attributes.UnMatch("/drafts/**", new ScriptObject { { "discard", true } });

        Assert.IsTrue(matchRule.Match);
        Assert.IsFalse(unmatchRule.Match);
        Assert.AreEqual("/blog/**/*.md", matchRule.Pattern);
        Assert.AreEqual("/drafts/**", unmatchRule.Pattern);
    }

    [Test]
    public void TestAttributesMatchRejectsInvalidArguments()
    {
        var attributes = new AttributesObject();
        var setters = new ScriptObject();

        Assert.Throws<ArgumentNullException>(() => attributes.Match(null!, setters));
        Assert.Throws<ArgumentNullException>(() => attributes.UnMatch("/blog/**", null!));
    }

    [Test]
    public void TestAttributesProcessorAppliesMatchingRulesInOrder()
    {
        var attributes = new AttributesObject();
        attributes.Clear();
        attributes.Match("/blog/**/*.md", new ScriptObject { { "layout", "blog" }, { "section", "blog" } });
        attributes.Match("/blog/special/*.md", new ScriptObject { { "layout", "special" } });

        ScriptObject? result = null;
        ProcessAttributes(attributes, (UPath)"/blog/special/post.md", ref result);

        Assert.NotNull(result);
        Assert.AreEqual("special", result!["layout"]);
        Assert.AreEqual("blog", result["section"]);
    }

    [Test]
    public void TestAttributesProcessorSupportsUnmatchRules()
    {
        var attributes = new AttributesObject();
        attributes.Clear();
        attributes.UnMatch("/drafts/**", new ScriptObject { { "discard", true } });

        ScriptObject? result = null;
        ProcessAttributes(attributes, (UPath)"/posts/post.md", ref result);

        Assert.NotNull(result);
        Assert.AreEqual(true, result!["discard"]);
    }

    [Test]
    public void TestAttributesProcessorLeavesObjectNullWhenNoRuleMatches()
    {
        var attributes = new AttributesObject();
        attributes.Clear();
        attributes.Match("/blog/**/*.md", new ScriptObject { { "layout", "blog" } });

        ScriptObject? result = null;
        ProcessAttributes(attributes, (UPath)"/assets/style.css", ref result);

        Assert.Null(result);
    }

    [Test]
    public void TestAttributesPluginRegistersBuiltinsAndPreloader()
    {
        using var context = new SiteTestContext();
        var plugin = new AttributesPlugin(context.Site);

        Assert.NotNull(plugin);
        Assert.IsInstanceOf<AttributesObject>(context.Site.Builtins["attributes"]);
        Assert.AreEqual(1, context.Site.Content.BeforeLoadingContentProcessors.Count);

        var processor = context.Site.Content.BeforeLoadingContentProcessors[0];
        ScriptObject? preContent = null;
        processor((UPath)"/blog/2024-01-02-my-post.md", ref preContent);

        Assert.NotNull(preContent);
        Assert.AreEqual("/:section/:year/:month/:day/:slug:output_ext", preContent!["url"]);
    }

    private static void ProcessAttributes(AttributesObject attributes, UPath path, ref ScriptObject? result)
    {
        var method = typeof(AttributesObject).GetMethod("ProcessAttributesForPath", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access AttributesObject.ProcessAttributesForPath");
        }

        var args = new object?[] { path, result };
        method.Invoke(attributes, args);
        result = args[1] as ScriptObject;
    }
}
