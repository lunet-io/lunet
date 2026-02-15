// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Infrastructure;

internal sealed class InMemorySiteFileSystems : SiteFileSystems
{
    public InMemorySiteFileSystems()
    {
        WorkspaceFileSystem = new MemoryFileSystem();
    }

    public MemoryFileSystem WorkspaceFileSystem { get; }

    public override void Initialize(string? inputDirectory = null, string? outputDirectory = null)
    {
        var inputPath = NormalizePath(inputDirectory);
        InputFileSystem = WorkspaceFileSystem.GetOrCreateSubFileSystem(inputPath);

        var outputPath = outputDirectory is not null
            ? NormalizePath(outputDirectory)
            : inputPath / LunetFolderName / BuildFolderName / DefaultOutputFolderName;
        OutputFileSystem = WorkspaceFileSystem.GetOrCreateSubFileSystem(outputPath);
    }

    public static UPath NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == ".")
        {
            return UPath.Root;
        }

        var normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith("./", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath[2..];
        }

        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return (UPath)normalizedPath;
    }
}
