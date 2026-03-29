// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using System.Net;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Api.DotNet;

[NonParallelizable]
public class TestApiDotNetBasePathOutput
{
    private PhysicalLunetAppTestContext _context = null!;
    private string _outputRoot = null!;

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        _context = new PhysicalLunetAppTestContext();
        WriteTestSiteAndProject(_context);

        var siteDirectory = _context.GetAbsolutePath("site");
        var exitCode = await _context.RunAsync($"--input-dir={siteDirectory}", "build", "--dev");
        Assert.AreEqual(0, exitCode, "lunet build should succeed for basepath API output integration test.");

        _outputRoot = _context.GetAbsolutePath("site/.lunet/build/www");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public void TestApiPagesAreWrittenWithoutBasePathPrefix()
    {
        var rootPage = Path.Combine(_outputRoot, "api", "index.html");
        Assert.IsTrue(File.Exists(rootPage), $"Expected API root page at `{rootPage}`.");

        var namespacePage = Path.Combine(_outputRoot, "api", UidHelper.Handleize("BasePathApi"), "index.html");
        Assert.IsTrue(File.Exists(namespacePage), $"Expected API namespace page at `{namespacePage}`.");

        var incorrectRootPage = Path.Combine(_outputRoot, "terminal", "api", "index.html");
        Assert.IsFalse(File.Exists(incorrectRootPage), $"API pages must not be emitted under basepath-prefixed output `{incorrectRootPage}`.");
    }

    [Test]
    public void TestApiLinksStillIncludeConfiguredBasePath()
    {
        var rootPage = Path.Combine(_outputRoot, "api", "index.html");
        var html = WebUtility.HtmlDecode(File.ReadAllText(rootPage));

        StringAssert.Contains("/terminal/api/BasePathApi/", html);
    }

    [Test]
    public void TestApiInternalXrefsRenderAsLinks()
    {
        var consumerPage = Path.Combine(_outputRoot, "api", UidHelper.Handleize("BasePathApi.ApiConsumer"), "index.html");
        Assert.IsTrue(File.Exists(consumerPage), $"Expected API member page at `{consumerPage}`.");

        var html = WebUtility.HtmlDecode(File.ReadAllText(consumerPage));

        StringAssert.Contains("/terminal/api/BasePathApi.ApiSurface/", html);
    }

    private static void WriteTestSiteAndProject(PhysicalLunetAppTestContext context)
    {
        context.WriteAllText(
            "site/config.scriban",
            """
            baseurl = "https://example.test"
            basepath = "/terminal"
            title = "Basepath API test"

            with api.dotnet
              title = "Basepath API test"
              path = "/api"
              properties = { TargetFramework: "net10.0" }
              projects = [
                {
                  name: "BasePathApi",
                  path: "../src/BasePathApi/BasePathApi.csproj"
                }
              ]
            end
            """);

        context.WriteAllText(
            "site/readme.md",
            """
            ---
            title: Home
            ---

            # Home
            """);

        context.WriteAllText(
            "site/.lunet/layouts/_default.sbn-html",
            """
            <!doctype html>
            <html>
            <head>
            {{~ Head ~}}
            </head>
            <body>
            {{ content }}
            </body>
            </html>
            """);

        context.WriteAllText(
            "src/BasePathApi/BasePathApi.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>preview</LangVersion>
                <Nullable>enable</Nullable>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
            </Project>
            """);

        context.WriteAllText(
            "src/BasePathApi/ApiSurface.cs",
            """
            namespace BasePathApi;

            /// <summary>Simple API surface used to validate output paths.</summary>
            public class ApiSurface
            {
                /// <summary>Returns a stable value.</summary>
                public int GetValue() => 42;
            }

            /// <summary>Consumes <see cref="ApiSurface"/> and links back to it.</summary>
            public class ApiConsumer
            {
                /// <summary>Returns the provided <see cref="ApiSurface"/>.</summary>
                public ApiSurface Echo(ApiSurface value) => value;
            }
            """);
    }
}
