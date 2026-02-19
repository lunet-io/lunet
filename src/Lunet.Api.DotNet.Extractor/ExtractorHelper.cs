// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static string SelectBestResult(IReadOnlyList<string> results, string projectPath, string projectName, string targetFramework)
        {
            if (results.Count == 0)
            {
                return string.Empty;
            }

            if (results.Count == 1)
            {
                return results[0];
            }

            var normalizedProjectPath = NormalizePath(projectPath);
            var projectDirectory = Path.GetDirectoryName(normalizedProjectPath) ?? string.Empty;
            var projectFileName = Path.GetFileNameWithoutExtension(normalizedProjectPath);
            var expectedAssemblyName = string.IsNullOrWhiteSpace(projectName) ? projectFileName : projectName;

            var candidates = results.Select(result => new ResultCandidate(result)).ToList();

            candidates = Filter(candidates, candidate => IsPathUnderDirectory(candidate.FullPath, projectDirectory));
            candidates = FilterByTargetFramework(candidates, targetFramework);
            candidates = Filter(candidates, candidate => string.Equals(candidate.FileNameWithoutExtension, expectedAssemblyName, System.StringComparison.OrdinalIgnoreCase));
            candidates = Filter(candidates, candidate => string.Equals(candidate.FileNameWithoutExtension, projectFileName, System.StringComparison.OrdinalIgnoreCase));

            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            var bestCandidate = candidates
                .OrderBy(candidate => candidate.FullPath.Length)
                .ThenBy(candidate => candidate.FullPath, System.StringComparer.OrdinalIgnoreCase)
                .First();

            var hasStrongMatch = IsPathUnderDirectory(bestCandidate.FullPath, projectDirectory)
                || string.Equals(bestCandidate.FileNameWithoutExtension, expectedAssemblyName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(bestCandidate.FileNameWithoutExtension, projectFileName, System.StringComparison.OrdinalIgnoreCase);

            return hasStrongMatch ? bestCandidate.OriginalPath : string.Empty;
        }

        private static List<ResultCandidate> Filter(List<ResultCandidate> candidates, System.Func<ResultCandidate, bool> predicate)
        {
            var filteredCandidates = candidates.Where(predicate).ToList();
            return filteredCandidates.Count > 0 ? filteredCandidates : candidates;
        }

        private static List<ResultCandidate> FilterByTargetFramework(List<ResultCandidate> candidates, string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                return candidates;
            }

            targetFramework = targetFramework.Trim();
            return Filter(candidates, candidate =>
            {
                return candidate.FullPath.IndexOf(targetFramework, System.StringComparison.OrdinalIgnoreCase) >= 0;
            });
        }

        private static bool IsPathUnderDirectory(string path, string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            var normalizedDirectory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(path, normalizedDirectory, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var directoryWithSeparator = normalizedDirectory + Path.DirectorySeparatorChar;
            var directoryWithAlternateSeparator = normalizedDirectory + Path.AltDirectorySeparatorChar;
            return path.StartsWith(directoryWithSeparator, System.StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith(directoryWithAlternateSeparator, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private sealed class ResultCandidate
        {
            public ResultCandidate(string originalPath)
            {
                OriginalPath = originalPath;
                FullPath = NormalizePath(originalPath);
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(FullPath);
            }

            public string OriginalPath { get; }

            public string FullPath { get; }

            public string FileNameWithoutExtension { get; }
        }
    }
}
