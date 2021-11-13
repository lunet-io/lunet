// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Zio;
using Zio.FileSystems;

namespace Lunet.Core;

/// <summary>
/// Regroups all the FileSystems used to build a site (input and output).
/// </summary>
public class SiteFileSystems
{
    public const string CacheSiteFolderName = "cache";
    public static readonly UPath TempSiteFolder = UPath.Root / CacheSiteFolderName;

    public const string SharedFolderName = "shared";
    public static readonly UPath SiteFolder = UPath.Root / SharedFolderName;

    public const string LunetFolderName = ".lunet";
    public static readonly UPath LunetFolder = UPath.Root / LunetFolderName;
    public static readonly string LunetFolderWithSlash = "/" + LunetFolderName + "/";

    public const string ModulesFolderName = "modules";
        
    public const string BuildFolderName = "build";
    public static readonly UPath BuildFolder = LunetFolder / BuildFolderName;

    public const string DefaultOutputFolderName = "www";
    public const string DefaultConfigFileName = "config.scriban";

    private readonly AggregateFileSystem _fileSystem;
    private readonly AggregateFileSystem _metaFileSystem;
    private readonly List<IFileSystem> _contentFileSystems;
    private IFileSystem _inputFileSystem;

    public SiteFileSystems()
    {
        var sharedFolder = Path.Combine(AppContext.BaseDirectory, SharedFolderName);

        _contentFileSystems = new List<IFileSystem>();
        var sharedPhysicalFileSystem = new PhysicalFileSystem();

        // Make sure that SharedFileSystem is a read-only filesystem
        SharedFileSystem = new ReadOnlyFileSystem(new SubFileSystem(sharedPhysicalFileSystem, sharedPhysicalFileSystem.ConvertPathFromInternal(sharedFolder)));
        SharedMetaFileSystem = SharedFileSystem.GetOrCreateSubFileSystem(LunetFolder);

        _fileSystem = new AggregateFileSystem(SharedFileSystem);

        // MetaFileSystem provides an aggregate view of the shared meta file system + the user meta file system
        _metaFileSystem = new AggregateFileSystem(SharedMetaFileSystem);
        MetaFileSystem = _metaFileSystem;

        ConfigFile = new FileEntry(FileSystem, UPath.Root / DefaultConfigFileName);
    }

    public IFileSystem InputFileSystem
    {
        get => _inputFileSystem;
        set
        {
            _inputFileSystem = value;
            CacheSiteFileSystem = _inputFileSystem?.GetOrCreateSubFileSystem(LunetFolder / BuildFolderName / CacheSiteFolderName);
            CacheMetaFileSystem = _inputFileSystem?.GetOrCreateSubFileSystem(LunetFolder / BuildFolderName / CacheSiteFolderName / LunetFolderName);
            UpdateFileSystem();
        }
    }

    public FileEntry ConfigFile { get; }

    public FileSystem OutputFileSystem { get; set; }
        
    public IFileSystem CacheSiteFileSystem { get; private set; }

    public IFileSystem FileSystem => _fileSystem;

    public IFileSystem SharedFileSystem { get; }


    public IFileSystem SharedMetaFileSystem { get; }

    public IFileSystem CacheMetaFileSystem { get; private set; }

    public IFileSystem MetaFileSystem { get; }


    public void Initialize(string inputDirectory = null, string outputDirectory = null)
    {
        var diskFs = new PhysicalFileSystem();
        var rootFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, inputDirectory ?? "."));
        var siteFileSystem = new SubFileSystem(diskFs, diskFs.ConvertPathFromInternal(rootFolder));
        InputFileSystem = siteFileSystem;

        var outputFolder = outputDirectory != null
            ? Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, outputDirectory))
            : Path.Combine(rootFolder, SiteFileSystems.LunetFolderName + "/build/" + SiteFileSystems.DefaultOutputFolderName);

        var outputFolderForFs = diskFs.ConvertPathFromInternal(outputFolder);
        OutputFileSystem = diskFs.GetOrCreateSubFileSystem(outputFolderForFs);
    }

    public void ClearContentFileSystems()
    {
        _contentFileSystems.Clear();
    }

    public void AddContentFileSystem(IFileSystem fileSystem)
    {
        if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
        if (!_contentFileSystems.Contains(fileSystem))
        {
            _contentFileSystems.Add(fileSystem);
        }
        UpdateFileSystem();
    }

    private void UpdateFileSystem()
    {
        _fileSystem.ClearFileSystems();
        foreach (var contentFs in _contentFileSystems)
        {
            _fileSystem.AddFileSystem(contentFs);
        }
        if (InputFileSystem != null)
        {
            _fileSystem.AddFileSystem(InputFileSystem);
        }

        // Update _metaFileSystem
        _metaFileSystem.ClearFileSystems();
        if (CacheMetaFileSystem != null)
        {
            _metaFileSystem.AddFileSystem(CacheMetaFileSystem);
        }
        _metaFileSystem.AddFileSystem(new SubFileSystem(_fileSystem, LunetFolder));
    }
}