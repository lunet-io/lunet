// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Api.DotNet.Extractor;

namespace Lunet.Api.DotNet;

public static class ApiDotNetResultSelector
{
    public static string SelectBestResult(IReadOnlyList<string> results, string projectPath, string projectName, string targetFramework)
    {
        return ExtractorHelper.SelectBestResult(results, projectPath, projectName, targetFramework);
    }
}
