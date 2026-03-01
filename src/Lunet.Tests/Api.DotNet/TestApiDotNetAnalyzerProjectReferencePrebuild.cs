// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Api.DotNet;

[NonParallelizable]
public class TestApiDotNetAnalyzerProjectReferencePrebuild
{
    private PhysicalLunetAppTestContext _context = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _context = new PhysicalLunetAppTestContext();
        WriteTestSiteAndProjects(_context);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task TestApiDotNetBuildRetriesAfterPrebuildingAnalyzerProjectReference()
    {
        var analyzerDll = _context.GetAbsolutePath("src/AnalyzerRef/bin/Release/netstandard2.0/AnalyzerRef.dll");
        Assert.IsFalse(File.Exists(analyzerDll), "AnalyzerRef.dll should not exist before the first build.");

        var siteDirectory = _context.GetAbsolutePath("site");
        var exitCode = await _context.RunAsync($"--input-dir={siteDirectory}", "build", "--dev");
        Assert.AreEqual(0, exitCode, "lunet build should succeed even when analyzer ProjectReferences were not built yet.");

        Assert.IsTrue(File.Exists(analyzerDll), "AnalyzerRef.dll should be built as part of the prebuild retry.");
    }

    private static void WriteTestSiteAndProjects(PhysicalLunetAppTestContext context)
    {
        context.WriteAllText(
            "site/config.scriban",
            """
            baseurl = "https://example.test"
            title = "Analyzer prebuild retry test"

            with api.dotnet
              title = "Analyzer prebuild retry test"
              path = "/api"
              properties = { TargetFramework: "net10.0", BuildProjectReferences: false }
              projects = [
                {
                  name: "AnalyzerRetry",
                  path: "../src/AnalyzerRetry/AnalyzerRetry.csproj"
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
            "src/AnalyzerRef/AnalyzerRef.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.0</TargetFramework>
                <LangVersion>preview</LangVersion>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        context.WriteAllText(
            "src/AnalyzerRef/AnalyzerMarker.cs",
            """
            namespace AnalyzerRef;

            // This does not need to be a real analyzer. It is referenced as an analyzer-only project output
            // and must exist on disk for the compiler to load it.
            public static class AnalyzerMarker
            {
            }
            """);

        context.WriteAllText(
            "src/AnalyzerRetry/AnalyzerRetry.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>preview</LangVersion>
                <Nullable>enable</Nullable>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\AnalyzerRef\AnalyzerRef.csproj"
                                  OutputItemType="Analyzer"
                                  ReferenceOutputAssembly="false" />
              </ItemGroup>
            </Project>
            """);

        context.WriteAllText(
            "src/AnalyzerRetry/ApiSurface.cs",
            """
            namespace AnalyzerRetry;

            /// <summary>Simple API surface used for analyzer prebuild retry.</summary>
            public class ApiSurface
            {
                /// <summary>Returns a stable value.</summary>
                public int GetValue() => 42;
            }
            """);
    }
}

