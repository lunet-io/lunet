// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lunet.Core;
using Zio;

namespace Lunet.Tests.Infrastructure;

internal sealed class LunetAppTestContext
{
    private readonly LunetApp _app;
    private bool _hasRun;

    public LunetAppTestContext()
    {
        FileSystems = new InMemorySiteFileSystems();
        _app = new LunetApp(new SiteConfiguration(fileSystems: FileSystems));
    }

    public InMemorySiteFileSystems FileSystems { get; }

    public async Task<int> RunAsync(params string[] args)
    {
        if (_hasRun)
        {
            throw new InvalidOperationException("RunAsync can only be called once per test context.");
        }

        _hasRun = true;
        return await _app.RunAsync(args);
    }

    public bool FileExists(string path)
    {
        var normalizedPath = InMemorySiteFileSystems.NormalizePath(path);
        return FileSystems.WorkspaceFileSystem.FileExists(normalizedPath);
    }

    public string ReadAllText(string path)
    {
        var normalizedPath = InMemorySiteFileSystems.NormalizePath(path);
        return FileSystems.WorkspaceFileSystem.ReadAllText(normalizedPath);
    }

    public void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        var normalizedPath = InMemorySiteFileSystems.NormalizePath(path);
        var directory = normalizedPath.GetDirectory();
        if (!directory.IsNull)
        {
            FileSystems.WorkspaceFileSystem.CreateDirectory(directory);
        }
        FileSystems.WorkspaceFileSystem.WriteAllText(normalizedPath, content);
    }
}
