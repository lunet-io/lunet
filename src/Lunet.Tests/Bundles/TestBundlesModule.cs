// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Resources;
using Lunet.Tests.Infrastructure;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Bundles;

public class TestBundlesModule
{
    [Test]
    public void TestBundlePluginRegistersSiteBuiltinsAndProcessor()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);

        Assert.AreSame(plugin.List, context.Site[SiteVariables.Bundles]);
        CollectionAssert.Contains(context.Site.Html.Head.Includes, "_builtins/bundle.sbn-html");
        Assert.IsTrue(context.Site.Builtins.ContainsKey(GlobalVariables.BundleFunction));
        Assert.AreSame(plugin.BundleProcessor, context.Site.Content.BeforeProcessingProcessors.Find<BundleProcessor>());
    }

    [Test]
    public void TestGetOrCreateBundleUsesDefaultAndCachesByName()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);

        var defaultBundle = plugin.GetOrCreateBundle(null);
        var defaultBundleFromEmpty = plugin.GetOrCreateBundle("");
        var namedBundle = plugin.GetOrCreateBundle("docs");
        var namedBundleAgain = plugin.GetOrCreateBundle("docs");

        Assert.AreEqual(BundlePlugin.DefaultBundleName, defaultBundle.Name);
        Assert.AreSame(defaultBundle, defaultBundleFromEmpty);
        Assert.AreSame(namedBundle, namedBundleAgain);
        Assert.AreEqual(2, plugin.List.Count);
        Assert.AreSame(defaultBundle, plugin.FindBundle(null));
        Assert.AreSame(namedBundle, plugin.FindBundle("docs"));
    }

    [Test]
    public void TestBundleFunctionSupportsZeroOrOneArgument()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);

        var defaultBundle = InvokeBundleFunction(plugin) as BundleObject;
        var namedBundle = InvokeBundleFunction(plugin, "frontend") as BundleObject;

        Assert.NotNull(defaultBundle);
        Assert.NotNull(namedBundle);
        Assert.AreEqual(BundlePlugin.DefaultBundleName, defaultBundle!.Name);
        Assert.AreEqual("frontend", namedBundle!.Name);

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeBundleFunction(plugin, "a", "b"));
        Assert.IsInstanceOf<ArgumentException>(ex!.InnerException);
    }

    [Test]
    public void TestBundleObjectAddLinkValidations()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);
        var bundle = plugin.GetOrCreateBundle("docs");

        var exWildcard = Assert.Throws<ArgumentException>(() => bundle.AddLink(BundleObjectProperties.JsType, "/scripts/*.js", "/js"));
        StringAssert.Contains("Must end with a `/`", exWildcard!.Message);

        var exStringPath = Assert.Throws<ArgumentException>(() => bundle.AddJs("/scripts/app.js", "/custom.js"));
        StringAssert.Contains("Parameter must be null", exStringPath!.Message);
    }

    [Test]
    public void TestBundleObjectCanAddResourceMainLink()
    {
        using var context = new SiteTestContext();
        var bundlePlugin = new BundlePlugin(context.Site);
        var bundle = bundlePlugin.GetOrCreateBundle("docs");
        var resourcePlugin = new ResourcePlugin(context.Site);
        var provider = new DummyResourceProvider(resourcePlugin, "test");
        using var fs = new MemoryFileSystem();
        fs.CreateDirectory("/resources/test/pkg/1.0.0");
        var resource = new ResourceObject(provider, "pkg", "1.0.0", new DirectoryEntry(fs, "/resources/test/pkg/1.0.0"));
        resource["main"] = "/resources/test/pkg/1.0.0/index.js";

        bundle.AddJs(resource);

        Assert.AreEqual(1, bundle.Links.Count);
        Assert.AreEqual("/resources/test/pkg/1.0.0/index.js", bundle.Links[0].Path);
        Assert.AreEqual(BundleObjectProperties.JsType, bundle.Links[0].Type);
    }

    [Test]
    public void TestBundleProcessorResolvesBasicStaticLink()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);
        var bundle = plugin.GetOrCreateBundle(null);
        bundle.AddJs("/scripts/app.js");

        var staticFile = context.CreateFileContentObject("/scripts/app.js", "console.log('app');");
        var page = context.CreateDynamicContentObject("/index.html");
        context.Site.StaticFiles.Add(staticFile);
        context.Site.Pages.Add(page);

        plugin.BundleProcessor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(1, bundle.Links.Count);
        Assert.AreEqual("/scripts/app.js", bundle.Links[0].Path);
        Assert.NotNull(bundle.Links[0].Url);
        StringAssert.EndsWith("/js/app.js", bundle.Links[0].Url!);
        Assert.AreSame(staticFile, bundle.Links[0].ContentObject);
    }

    [Test]
    public void TestBundleProcessorConcatCreatesDynamicPage()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);
        var bundle = plugin.GetOrCreateBundle(null);
        bundle.Concat = true;
        bundle.AddJs("/scripts/a.js");
        bundle.AddJs("/scripts/b.js");

        context.WriteInputFile("/scripts/a.js", "const a = 1;");
        context.WriteInputFile("/scripts/b.js", "const b = 2;");
        var page = context.CreateDynamicContentObject("/index.html");
        context.Site.Pages.Add(page);

        plugin.BundleProcessor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(1, bundle.Links.Count);
        Assert.IsNull(bundle.Links[0].Path);
        Assert.NotNull(bundle.Links[0].ContentObject);
        Assert.IsNotEmpty(bundle.Links[0].Content);
        StringAssert.Contains("const a = 1;", bundle.Links[0].Content!);
        StringAssert.Contains("const b = 2;", bundle.Links[0].Content!);
        Assert.AreEqual(1, context.Site.DynamicPages.Count);
        Assert.AreEqual(0, context.Site.StaticFiles.Count);
    }

    [Test]
    public void TestBundleProcessorMinifyUsesRegisteredMinifier()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);
        var minifier = new RecordingMinifier("test");
        plugin.BundleProcessor.Minifiers.Add(minifier);

        var bundle = plugin.GetOrCreateBundle(null);
        bundle.Minify = true;
        bundle.Minifier = "test";
        bundle.AddJs("/scripts/app.js");

        context.WriteInputFile("/scripts/app.js", "const value = 1;");
        var page = context.CreateDynamicContentObject("/index.html");
        context.Site.Pages.Add(page);

        plugin.BundleProcessor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(1, minifier.MinifyCalls.Count);
        Assert.AreEqual("js", minifier.MinifyCalls[0].Type);
        Assert.AreEqual("/scripts/app.js", minifier.MinifyCalls[0].Path);
        Assert.AreEqual("minified(js):const value = 1;", bundle.Links[0].Content);
        StringAssert.EndsWith(".min.js", bundle.Links[0].Url!);
    }

    [Test]
    public void TestBundleProcessorContentLinkToFolderAppendsFileName()
    {
        using var context = new SiteTestContext();
        var plugin = new BundlePlugin(context.Site);
        var bundle = plugin.GetOrCreateBundle(null);
        bundle.AddContent("/scripts/app.js", "/assets/components/");

        context.WriteInputFile("/scripts/app.js", "console.log('app');");
        context.Site.Pages.Add(context.CreateDynamicContentObject("/index.html"));

        plugin.BundleProcessor.Process(ProcessingStage.BeforeProcessingContent);

        Assert.AreEqual(1, bundle.Links.Count);
        Assert.AreEqual("/assets/components/app.js", bundle.Links[0].UrlWithoutBasePath);
        StringAssert.EndsWith("/assets/components/app.js", bundle.Links[0].Url!);
    }

    private static object? InvokeBundleFunction(BundlePlugin plugin, params object[] args)
    {
        var method = typeof(BundlePlugin).GetMethod("BundleFunction", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access BundlePlugin.BundleFunction");
        }

        return method.Invoke(plugin, new object[] { args });
    }

    private sealed class RecordingMinifier : IContentMinifier
    {
        public RecordingMinifier(string name)
        {
            Name = name;
            MinifyCalls = new List<(string Type, string Content, string? Path)>();
        }

        public string Name { get; }

        public List<(string Type, string Content, string? Path)> MinifyCalls { get; }

        public string Minify(string type, string content, string? contentPath = null)
        {
            MinifyCalls.Add((type, content, contentPath));
            return $"minified({type}):{content}";
        }
    }

    private sealed class DummyResourceProvider : ResourceProvider
    {
        public DummyResourceProvider(ResourcePlugin plugin, string name) : base(plugin, name)
        {
        }

        protected override ResourceObject LoadFromDisk(string resourceName, string resourceVersion, DirectoryEntry directory)
        {
            return new ResourceObject(this, resourceName, resourceVersion, directory);
        }

        protected override ResourceObject InstallToDisk(string resourceName, string resourceVersion, DirectoryEntry directory, ResourceInstallFlags flags)
        {
            return new ResourceObject(this, resourceName, resourceVersion, directory);
        }
    }
}
