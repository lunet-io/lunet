// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Server;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Server;

public class TestServerModule
{
    [Test]
    public void TestServerPluginEnablesServerLoggingAndLiveReload()
    {
        using var context = new SiteTestContext();
        _ = new ServerPlugin(context.Site);

        Assert.IsTrue(context.Site.Builtins.LogObject.GetSafeValue<bool>("server"));
        Assert.IsTrue(context.Site.GetLiveReload());
    }

    [Test]
    public void TestSiteServerServiceUpdateTracksChangedValues()
    {
        using var context = new SiteTestContext();
        _ = new ServerPlugin(context.Site);
        context.Site.BaseUrl = "http://localhost:4321";
        context.Site.BasePath = "/docs";
        context.Site.Environment = "dev";
        context.Site.ErrorRedirect = "/404.html";

        var service = new SiteServerService(context.Configuration);

        var firstUpdateNeedsRestart = service.Update(context.Site);
        var secondUpdateNeedsRestart = service.Update(context.Site);
        context.Site.SetLiveReload(false);
        var thirdUpdateNeedsRestart = service.Update(context.Site);

        Assert.IsTrue(firstUpdateNeedsRestart);
        Assert.IsTrue(secondUpdateNeedsRestart);
        Assert.IsTrue(thirdUpdateNeedsRestart);
        Assert.AreEqual("http://localhost:4321", service.BaseUrl);
        Assert.AreEqual("/docs", service.BasePath);
        Assert.AreEqual("dev", service.Environment);
        Assert.AreEqual("/404.html", service.ErrorRedirect);
        Assert.IsFalse(service.LiveReload);
    }

    [Test]
    public void TestSetupLiveReloadClientAddsIncludeAndUrl()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "http://localhost:5000";

        SiteServerService.SetupLiveReloadClient(context.Site);

        CollectionAssert.Contains(context.Site.Html.Head.Includes, "_builtins/livereload.sbn-html");
        var liveReloadUrl = context.Site.GetSafeValue<string>("livereload_url");
        Assert.NotNull(liveReloadUrl);
        StringAssert.StartsWith("ws://localhost:5000", liveReloadUrl!);
        StringAssert.EndsWith("/__livereload__", liveReloadUrl!);
    }

    [Test]
    public void TestServeCommandRunnerDefaultOptions()
    {
        var runner = new ServeCommandRunner();

        Assert.IsTrue(runner.Watch);
        Assert.IsTrue(runner.Development);
    }
}
