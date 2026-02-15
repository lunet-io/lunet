// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Tests.Infrastructure;
using Lunet.Watcher;

namespace Lunet.Tests.Watcher;

public class TestWatcherModule
{
    [Test]
    public async Task TestBuildCommandRunnerSetsEnvironmentAndSingleThreadedOptions()
    {
        using var context = new SiteTestContext();
        var siteRunner = new SiteRunner(context.Configuration);
        siteRunner.CommandRunners.Clear();
        var buildRunner = new CapturingBuildCommandRunner
        {
            Watch = false,
            SingleThreaded = true,
            Development = true,
            ResultToReturn = RunnerResult.Exit
        };
        siteRunner.CommandRunners.Add(buildRunner);

        var result = await siteRunner.RunAsync();

        Assert.AreEqual(0, result);
        Assert.AreEqual("dev", buildRunner.CapturedEnvironment);
        Assert.IsTrue(buildRunner.CapturedSingleThreaded);
        Assert.IsTrue(siteRunner.Config.SingleThreaded);
    }

    [Test]
    public async Task TestBuildCommandRunnerFailsWhenSiteIsNotInitialized()
    {
        using var context = new SiteTestContext();
        var siteRunner = new SiteRunner(context.Configuration);
        var buildRunner = new BuildCommandRunner();

        var result = await buildRunner.RunAsync(siteRunner, CancellationToken.None);

        Assert.AreEqual(RunnerResult.ExitWithError, result);
    }

    [Test]
    public async Task TestSiteWatcherRunAsyncReturnsContinueWhenBatchExists()
    {
        using var context = new SiteTestContext();
        var siteRunner = new SiteRunner(context.Configuration);
        SetCurrentSite(siteRunner, context.Site);
        var watcherService = new SiteWatcherService(context.Configuration);
        siteRunner.RegisterService(watcherService);
        watcherService.FileSystemEvents.Add(new FileSystemEventBatchArgs());

        var result = await SiteWatcherService.RunAsync(siteRunner, CancellationToken.None);

        Assert.AreEqual(RunnerResult.Continue, result);
        watcherService.Dispose();
    }

    [Test]
    public async Task TestSiteWatcherRunAsyncReturnsExitOnCancellation()
    {
        using var context = new SiteTestContext();
        var siteRunner = new SiteRunner(context.Configuration);
        SetCurrentSite(siteRunner, context.Site);
        var watcherService = new SiteWatcherService(context.Configuration);
        siteRunner.RegisterService(watcherService);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await SiteWatcherService.RunAsync(siteRunner, cts.Token);

        Assert.AreEqual(RunnerResult.Exit, result);
    }

    [Test]
    public void TestFileSystemEventBatchArgsCreatesEmptyList()
    {
        var args = new FileSystemEventBatchArgs();

        Assert.NotNull(args.FileEvents);
        Assert.AreEqual(0, args.FileEvents.Count);
    }

    private static void SetCurrentSite(SiteRunner runner, SiteObject site)
    {
        var property = typeof(SiteRunner).GetProperty("CurrentSite", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null)
        {
            throw new InvalidOperationException("Unable to access SiteRunner.CurrentSite");
        }

        property.SetValue(runner, site);
    }

    private sealed class CapturingBuildCommandRunner : BuildCommandRunner
    {
        public RunnerResult ResultToReturn { get; set; }

        public string? CapturedEnvironment { get; private set; }

        public bool CapturedSingleThreaded { get; private set; }

        protected override Task<RunnerResult> RunAsyncImpl(SiteRunner runner, CancellationToken cancellationToken)
        {
            CapturedEnvironment = runner.CurrentSite?.Environment;
            CapturedSingleThreaded = runner.Config.SingleThreaded;
            return Task.FromResult(ResultToReturn);
        }
    }
}
