// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Lunet.Core;

namespace Lunet.Tests.Infrastructure;

internal sealed class PhysicalLunetAppTestContext : IDisposable
{
    public PhysicalLunetAppTestContext()
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), $"lunet-app-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootDirectory);
    }

    public string RootDirectory { get; }

    public string GetAbsolutePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(RootDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
    }

    public void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = GetAbsolutePath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(GetAbsolutePath(path));
    }

    public bool FileExists(string path)
    {
        return File.Exists(GetAbsolutePath(path));
    }

    public async Task<int> RunAsync(params string[] args)
    {
        var app = new LunetApp(new SiteConfiguration(new SiteLoggerFactory(defaultConsole: false)));
        return await app.RunAsync(args);
    }

    public void Dispose()
    {
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
