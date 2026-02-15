// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Lunet.Scripts;
using Zio;
using Zio.FileSystems;

namespace Lunet.Tests.Infrastructure;

internal sealed class SiteTestContext : IDisposable
{
    public SiteTestContext(string? inputDirectory = null, string? outputDirectory = null)
    {
        FileSystems = new InMemorySiteFileSystems();
        Configuration = new SiteConfiguration(new SiteLoggerFactory(defaultConsole: false), FileSystems);
        FileSystems.Initialize(inputDirectory, outputDirectory);
        Site = new SiteObject(Configuration);
    }

    public InMemorySiteFileSystems FileSystems { get; }

    public SiteConfiguration Configuration { get; }

    public SiteObject Site { get; }

    public UPath NormalizePath(string path)
    {
        return InMemorySiteFileSystems.NormalizePath(path);
    }

    public void WriteInputFile(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        var normalizedPath = NormalizePath(path);
        var directory = normalizedPath.GetDirectory();
        if (!directory.IsNull)
        {
            Site.SiteFileSystem.CreateDirectory(directory);
        }
        new FileEntry(Site.SiteFileSystem, normalizedPath).WriteAllText(content);
    }

    public bool OutputFileExists(string path)
    {
        return new FileEntry(Site.OutputFileSystem, NormalizePath(path)).Exists;
    }

    public string ReadOutputFile(string path)
    {
        return new FileEntry(Site.OutputFileSystem, NormalizePath(path)).ReadAllText();
    }

    public FileContentObject CreateFileContentObject(string path, string content, bool withFrontMatterScript = false)
    {
        var normalizedPath = NormalizePath(path);
        WriteInputFile(path, content);

        var sourceFile = new FileSystemItem(Site.SiteFileSystem, normalizedPath, false);
        ScriptInstance? scriptInstance = null;
        if (withFrontMatterScript)
        {
            scriptInstance = Site.Scripts.ParseScript(content, normalizedPath, Scriban.Parsing.ScriptMode.FrontMatterAndContent);
            if (scriptInstance.HasErrors)
            {
                throw new InvalidOperationException($"Invalid script content for [{normalizedPath}]");
            }
        }

        return new FileContentObject(Site, sourceFile, scriptInstance);
    }

    public DynamicContentObject CreateDynamicContentObject(string url, string? section = null, string? path = null)
    {
        UPath? dynamicPath = path is null ? default(UPath?) : NormalizePath(path);
        return new DynamicContentObject(Site, url, section, dynamicPath);
    }

    public void Dispose()
    {
        Configuration.LoggerFactory.Dispose();
    }
}
