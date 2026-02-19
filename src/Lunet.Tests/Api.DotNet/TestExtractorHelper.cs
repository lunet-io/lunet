// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Lunet.Api.DotNet;

namespace Lunet.Tests.Api.DotNet;

public class TestExtractorHelper
{
    [Test]
    public void TestSelectBestResultPrefersRequestedProjectOutput()
    {
        var root = GetTestRootPath();
        var projectPath = Path.Combine(root, "src", "XenoAtom.Logging", "XenoAtom.Logging.csproj");
        var expected = Path.Combine(root, "src", "XenoAtom.Logging", "obj", "Release", "net10.0", "XenoAtom.Logging.api.json");
        var results = new List<string>
        {
            Path.Combine(root, "src", "XenoAtom.Logging.Generators", "obj", "Release", "netstandard2.0", "XenoAtom.Logging.Generators.api.json"),
            expected
        };

        var selectedResult = ApiDotNetResultSelector.SelectBestResult(results, projectPath, "XenoAtom.Logging", "net10.0");

        Assert.AreEqual(expected, selectedResult);
    }

    [Test]
    public void TestSelectBestResultPrefersTargetFrameworkWhenAvailable()
    {
        var root = GetTestRootPath();
        var projectPath = Path.Combine(root, "src", "Library", "Library.csproj");
        var expected = Path.Combine(root, "src", "Library", "obj", "Release", "net10.0", "Library.api.json");
        var results = new List<string>
        {
            Path.Combine(root, "src", "Library", "obj", "Release", "net8.0", "Library.api.json"),
            expected
        };

        var selectedResult = ApiDotNetResultSelector.SelectBestResult(results, projectPath, "Library", "net10.0");

        Assert.AreEqual(expected, selectedResult);
    }

    [Test]
    public void TestSelectBestResultReturnsEmptyWhenNoStrongMatch()
    {
        var root = GetTestRootPath();
        var projectPath = Path.Combine(root, "src", "Library", "Library.csproj");
        var results = new List<string>
        {
            Path.Combine(root, "src", "Other", "obj", "Release", "net10.0", "Other.api.json"),
            Path.Combine(root, "src", "Another", "obj", "Release", "net10.0", "Another.api.json")
        };

        var selectedResult = ApiDotNetResultSelector.SelectBestResult(results, projectPath, "Library", "net10.0");

        Assert.AreEqual(string.Empty, selectedResult);
    }

    private static string GetTestRootPath()
    {
        return Path.Combine(Path.GetTempPath(), "lunet-api-dotnet-selector-tests");
    }
}
