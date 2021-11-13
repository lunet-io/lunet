// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lunet.Api.DotNet.Extractor
{
    public static class ExtractorHelper
    {
        public const string ResultId = "XDoc0001";
        private static readonly Regex MatchResults = new Regex($@"warning\s+{ResultId}:\s+""((?:\\""|[^\""])*)");

        public static string FormatResult(string path)
        {
            return $"\"{path.Replace("\"", "\\\"")}\"";
        }

        public static List<string> FindResults(string text)
        {
            var matches = MatchResults.Matches(text);
            var results = new List<string>();
            foreach (Match match in matches)
            {
                results.Add(match.Groups[1].Value);
            }

            return results;
        }
    }
}