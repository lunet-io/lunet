// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Reflection;
using Lunet.Core;
using Lunet.Extends;
using Lunet.Tests.Infrastructure;
using Scriban.Runtime;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Extends;

public class TestExtendsModule
{
    [Test]
    public void TestExtendsPluginRegistersBuiltinsAndDefaults()
    {
        using var context = new SiteTestContext();
        var plugin = new ExtendsPlugin(context.Site);

        Assert.NotNull(plugin);
        Assert.AreEqual("/extends", (string)plugin.ExtendsFolder);
        Assert.AreEqual("/extends", (string)plugin.PrivateExtendsFolder);
        Assert.IsTrue(context.Site.Builtins.ContainsKey(SiteVariables.Extends));
        Assert.IsTrue(context.Site.Builtins.ContainsKey(SiteVariables.ExtendFunction));
        Assert.AreEqual(0, plugin.CurrentList.Count);
    }

    [Test]
    public void TestTryInstallLoadsLocalExtensionFromLunetFolder()
    {
        using var context = new SiteTestContext();
        var plugin = new ExtendsPlugin(context.Site);
        context.WriteInputFile("/.lunet/extends/theme/config.scriban", "");
        context.WriteInputFile("/.lunet/extends/theme/assets/site.css", "body { color: red; }");

        var extension = plugin.TryInstall("theme", isPrivate: false);

        Assert.NotNull(extension);
        Assert.AreEqual("theme", extension!.Name);
        Assert.AreEqual("theme", extension.FullName);
        Assert.IsNull(extension.Url);
        Assert.IsTrue(new FileEntry(extension.FileSystem, "/assets/site.css").Exists);
    }

    [Test]
    public void TestLoadExtendCachesEntryAndAddsContentFileSystem()
    {
        using var context = new SiteTestContext();
        var plugin = new ExtendsPlugin(context.Site);
        context.WriteInputFile("/.lunet/extends/theme/config.scriban", "");
        context.WriteInputFile("/.lunet/extends/theme/content/hello.txt", "hello");

        var first = plugin.LoadExtend("theme", isPrivate: false);
        var second = plugin.LoadExtend("theme", isPrivate: false);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.AreSame(first, second);
        Assert.AreEqual(1, plugin.CurrentList.Count);
        Assert.IsTrue(context.Site.FileSystem.FileExists("/content/hello.txt"));
    }

    [Test]
    public void TestParseQueryFromStringSupportsGitHubAndTag()
    {
        using var context = new SiteTestContext();
        var plugin = new ExtendsPlugin(context.Site);

        var request = InvokeParseQuery(plugin, "XenoAtom/lunet_template@1.2.3", isPrivate: true);

        Assert.AreEqual("lunet_template", GetRequestProperty<string>(request, "Name"));
        Assert.AreEqual("XenoAtom/lunet_template@1.2.3", GetRequestProperty<string>(request, "FullName"));
        Assert.AreEqual("XenoAtom/lunet_template", GetRequestProperty<string>(request, "Repository"));
        Assert.AreEqual("XenoAtom", GetRequestProperty<string>(request, "Owner"));
        Assert.AreEqual("lunet_template", GetRequestProperty<string>(request, "RepositoryName"));
        Assert.AreEqual("1.2.3", GetRequestProperty<string>(request, "Tag"));
        Assert.AreEqual("dist", GetRequestProperty<string>(request, "Directory"));
        Assert.IsTrue(GetRequestProperty<bool>(request, "IsGitHub"));
        Assert.IsTrue(GetRequestProperty<bool>(request, "IsPrivate"));
    }

    [Test]
    public void TestParseQueryFromScriptObjectSupportsDirectoryAndPublicFlag()
    {
        using var context = new SiteTestContext();
        var plugin = new ExtendsPlugin(context.Site);
        var query = new ScriptObject
        {
            ["repo"] = "https://github.com/XenoAtom/lunet_template",
            ["tag"] = "1.0.0",
            ["directory"] = "template/dist",
            ["public"] = true
        };

        var request = InvokeParseObjectQuery(plugin, query);

        Assert.AreEqual("lunet_template", GetRequestProperty<string>(request, "Name"));
        Assert.AreEqual("XenoAtom/lunet_template@1.0.0:template/dist", GetRequestProperty<string>(request, "FullName"));
        Assert.AreEqual("XenoAtom/lunet_template", GetRequestProperty<string>(request, "Repository"));
        Assert.AreEqual("template/dist", GetRequestProperty<string>(request, "Directory"));
        Assert.IsFalse(GetRequestProperty<bool>(request, "IsPrivate"));
    }

    [Test]
    public void TestParseQueryRejectsInvalidRepository()
    {
        using var context = new SiteTestContext();
        var plugin = new ExtendsPlugin(context.Site);
        var query = new ScriptObject
        {
            ["repo"] = "https://gitlab.com/XenoAtom/lunet_template"
        };

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseObjectQuery(plugin, query));
        Assert.IsInstanceOf<LunetException>(ex!.InnerException);
    }

    private static object InvokeParseQuery(ExtendsPlugin plugin, string value, bool isPrivate)
    {
        var method = typeof(ExtendsPlugin).GetMethod("ParseQuery", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(string), typeof(bool)], null);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access ExtendsPlugin.ParseQuery(string, bool)");
        }

        return method.Invoke(plugin, [value, isPrivate])!;
    }

    private static object InvokeParseObjectQuery(ExtendsPlugin plugin, object value)
    {
        var method = typeof(ExtendsPlugin).GetMethod("ParseQuery", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(object)], null);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access ExtendsPlugin.ParseQuery(object)");
        }

        return method.Invoke(plugin, [value])!;
    }

    private static T GetRequestProperty<T>(object request, string propertyName)
    {
        var property = request.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            throw new InvalidOperationException($"Unable to access request property {propertyName}");
        }

        return (T)property.GetValue(request)!;
    }
}
