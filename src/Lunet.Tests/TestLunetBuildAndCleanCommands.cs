// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading.Tasks;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests;

public class TestLunetBuildAndCleanCommands
{
    [Test]
    public async Task TestBuildFailsWhenConfigIsMissing()
    {
        var context = new LunetAppTestContext();

        var exitCode = await context.RunAsync("--input-dir=site", "build");

        Assert.AreEqual(1, exitCode);
    }

    [Test]
    public async Task TestBuildCopiesStaticFilesToDefaultOutput()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText(
            "/site/config.scriban",
            """
            baseurl = "https://example.com"
            """);
        context.WriteAllText("/site/assets/app.js", "console.log('ok');");

        var exitCode = await context.RunAsync("--input-dir=site", "build");

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(context.FileExists("/site/.lunet/build/www/assets/app.js"));
        Assert.AreEqual("console.log('ok');", context.ReadAllText("/site/.lunet/build/www/assets/app.js"));
    }

    [Test]
    public async Task TestCleanDeletesBuildFolderWhenConfigExists()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/config.scriban", "");
        context.WriteAllText("/site/.lunet/build/www/old.txt", "stale");

        var exitCode = await context.RunAsync("--input-dir=site", "clean");

        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(context.FileExists("/site/.lunet/build/www/old.txt"));
    }

    [Test]
    public async Task TestCleanFailsWhenConfigIsMissing()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/.lunet/build/www/old.txt", "stale");

        var exitCode = await context.RunAsync("--input-dir=site", "clean");

        Assert.AreEqual(1, exitCode);
    }
}
