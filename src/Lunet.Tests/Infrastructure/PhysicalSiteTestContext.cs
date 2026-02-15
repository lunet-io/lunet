// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using Lunet.Core;

namespace Lunet.Tests.Infrastructure;

internal sealed class PhysicalSiteTestContext : IDisposable
{
    public PhysicalSiteTestContext()
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), $"lunet-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootDirectory);

        Configuration = new SiteConfiguration(new SiteLoggerFactory(defaultConsole: false));
        Configuration.FileSystems.Initialize(RootDirectory);
        Site = new SiteObject(Configuration);
    }

    public string RootDirectory { get; }

    public SiteConfiguration Configuration { get; }

    public SiteObject Site { get; }

    public void WriteMetaFile(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        var relativePath = path.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(RootDirectory, SiteFileSystems.LunetFolderName, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        Configuration.LoggerFactory.Dispose();
        try
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, true);
            }
        }
        catch
        {
        }
    }
}
