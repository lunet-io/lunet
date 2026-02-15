// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Reflection;
using Lunet.Core;
using Lunet.Resources;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;
using Zio;

namespace Lunet.Tests.Resources;

public class TestResourcesModule
{
    [Test]
    public void TestResourcePluginRegistersDefaults()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);

        Assert.AreSame(plugin, context.Site[SiteVariables.Resources]);
        Assert.AreEqual(1, plugin.Providers.Count);
        Assert.IsInstanceOf<NpmResourceProvider>(plugin.Providers[0]);
        Assert.IsTrue(context.Site.Builtins.ContainsKey(SiteVariables.ResourceFunction));
    }

    [Test]
    public void TestTryLoadResourceReturnsNullForUnknownProvider()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);

        var result = plugin.TryLoadResource("unknown", "pkg", "1.0.0");

        Assert.Null(result);
    }

    [Test]
    public void TestResourceProviderLoadsFromMetaFileSystemWhenPresent()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        plugin.Providers.Clear();
        var provider = new RecordingProvider(plugin, "test");
        plugin.Providers.Add(provider);
        context.WriteInputFile("/.lunet/resources/test/pkg/1.0.0/.keep", "1");

        var first = provider.GetOrInstall("pkg", "1.0.0", ResourceInstallFlags.NoInstall);
        var second = provider.GetOrInstall("pkg", "1.0.0", ResourceInstallFlags.NoInstall);

        Assert.NotNull(first);
        Assert.AreSame(first, second);
        Assert.AreEqual(1, provider.LoadFromDiskCount);
        Assert.AreEqual(0, provider.InstallToDiskCount);
    }

    [Test]
    public void TestResourceProviderInstallsWhenMissing()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        plugin.Providers.Clear();
        var provider = new RecordingProvider(plugin, "test");
        plugin.Providers.Add(provider);

        var resource = provider.GetOrInstall("pkg", "2.0.0", ResourceInstallFlags.None);

        Assert.NotNull(resource);
        Assert.AreEqual(0, provider.LoadFromDiskCount);
        Assert.AreEqual(1, provider.InstallToDiskCount);
        Assert.AreEqual("/resources/test/pkg/2.0.0", resource!.Path.ToString());
    }

    [Test]
    public void TestResourceProviderRespectsNoInstallFlag()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        plugin.Providers.Clear();
        var provider = new RecordingProvider(plugin, "test");
        plugin.Providers.Add(provider);

        var resource = provider.GetOrInstall("pkg", "3.0.0", ResourceInstallFlags.NoInstall);

        Assert.Null(resource);
        Assert.AreEqual(0, provider.LoadFromDiskCount);
        Assert.AreEqual(0, provider.InstallToDiskCount);
    }

    [Test]
    public void TestResourceBuiltinParsesQueryAndObjectArguments()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);
        plugin.Providers.Clear();
        var provider = new RecordingProvider(plugin, "test");
        plugin.Providers.Add(provider);

        var fromString = InvokeResourceFunction(plugin, "test:pkg", "1.0.0") as ResourceObject;
        Assert.NotNull(fromString);
        Assert.AreEqual("pkg", provider.LastName);
        Assert.AreEqual("1.0.0", provider.LastVersion);
        Assert.AreEqual(ResourceInstallFlags.Private, provider.LastFlags);

        var queryObject = new ScriptObject
        {
            { "provider", "test" },
            { "name", "pkg-public" },
            { "version", "2.0.0-beta.1" },
            { "public", true },
            { "pre_release", true },
        };

        var fromObject = InvokeResourceFunction(plugin, queryObject, null) as ResourceObject;
        Assert.NotNull(fromObject);
        Assert.AreEqual("pkg-public", provider.LastName);
        Assert.AreEqual("2.0.0-beta.1", provider.LastVersion);
        Assert.AreEqual(ResourceInstallFlags.PreRelease, provider.LastFlags);
    }

    [Test]
    public void TestResourceBuiltinRejectsInvalidQuery()
    {
        using var context = new SiteTestContext();
        var plugin = new ResourcePlugin(context.Site);

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeResourceFunction(plugin, "invalid-query", null));

        Assert.IsInstanceOf<LunetException>(ex!.InnerException);
        StringAssert.Contains("Invalid resource name to load", ex.InnerException!.Message);
    }

    private static object? InvokeResourceFunction(ResourcePlugin plugin, object query, string? version)
    {
        var method = typeof(ResourcePlugin).GetMethod("ResourceFunction", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access ResourcePlugin.ResourceFunction");
        }

        return method.Invoke(plugin, new[] { query, version });
    }

    private sealed class RecordingProvider : ResourceProvider
    {
        public RecordingProvider(ResourcePlugin plugin, string name) : base(plugin, name)
        {
        }

        public int LoadFromDiskCount { get; private set; }

        public int InstallToDiskCount { get; private set; }

        public string? LastName { get; private set; }

        public string? LastVersion { get; private set; }

        public ResourceInstallFlags LastFlags { get; private set; }

        protected override ResourceObject LoadFromDisk(string resourceName, string resourceVersion, DirectoryEntry directory)
        {
            LoadFromDiskCount++;
            LastName = resourceName;
            LastVersion = resourceVersion;
            return new ResourceObject(this, resourceName, resourceVersion, directory);
        }

        protected override ResourceObject InstallToDisk(string resourceName, string resourceVersion, DirectoryEntry directory, ResourceInstallFlags flags)
        {
            InstallToDiskCount++;
            LastName = resourceName;
            LastVersion = resourceVersion;
            LastFlags = flags;
            return new ResourceObject(this, resourceName, resourceVersion, directory);
        }
    }
}
