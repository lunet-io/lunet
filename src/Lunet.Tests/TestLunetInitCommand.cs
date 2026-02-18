// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading.Tasks;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests;

public class TestLunetInitCommand
{
    [Test]
    public async Task TestInitCreatesWebsiteTemplateInMemory()
    {
        var context = new LunetAppTestContext();

        var exitCode = await context.RunAsync("init");

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(context.FileExists("/config.scriban"));
        Assert.IsTrue(context.FileExists("/readme.md"));
        Assert.IsTrue(context.FileExists("/menu.yml"));
        Assert.IsTrue(context.FileExists("/docs/menu.yml"));
        Assert.IsTrue(context.FileExists("/docs/readme.md"));
        Assert.IsNotEmpty(context.ReadAllText("/config.scriban"));
    }

    [Test]
    public async Task TestInitFailsWhenDestinationIsNotEmptyWithoutForce()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/existing.txt", "existing file");

        var exitCode = await context.RunAsync("init");

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(context.FileExists("/config.scriban"));
    }

    [Test]
    public async Task TestInitForceCreatesWebsiteTemplateInNonEmptyFolder()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/existing.txt", "existing file");

        var exitCode = await context.RunAsync("init", "--force");

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(context.FileExists("/existing.txt"));
        Assert.IsTrue(context.FileExists("/config.scriban"));
        Assert.IsTrue(context.FileExists("/readme.md"));
        Assert.IsTrue(context.FileExists("/menu.yml"));
        Assert.IsTrue(context.FileExists("/docs/menu.yml"));
        Assert.IsTrue(context.FileExists("/docs/readme.md"));
    }
}
