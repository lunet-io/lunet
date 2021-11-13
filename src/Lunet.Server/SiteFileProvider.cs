// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Zio;

namespace Lunet.Server;

/// <summary>
/// Provides a IFileProvider implementation on top of Zio
/// </summary>
internal class SiteFileProvider : IFileProvider
{
    private readonly IFileSystem _fs;

    public SiteFileProvider(IFileSystem fileSystem)
    {
        _fs = fileSystem;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return new SiteFileInfo(_fs, subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new SiteDirectoryContents(_fs, subpath);
    }

    public IChangeToken Watch(string filter)
    {
        // TODO: Not sure we actually need this for now
        return new SiteChangeToken(_fs, filter);
    }

    private class SiteFileInfo : IFileInfo
    {
        private readonly IFileSystem _fileSystem;
        private UPath _path;

        public SiteFileInfo(IFileSystem fileSystem, UPath path)
        {
            _fileSystem = fileSystem;
            _path = path;
        }

        public Stream CreateReadStream()
        {
            return _fileSystem.OpenFile(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool Exists => _fileSystem.FileExists(_path);

        public long Length => _fileSystem.GetFileLength(_path);

        public string PhysicalPath => _fileSystem.ConvertPathToInternal(_path);

        public string Name => _path.GetName();

        public DateTimeOffset LastModified => _fileSystem.GetLastWriteTime(_path);

        public bool IsDirectory => _fileSystem.DirectoryExists(_path);
    }

    private class SiteChangeToken : IChangeToken
    {
        private readonly IFileSystem _fs;
        private string _filter;

        public SiteChangeToken(IFileSystem filesystem, string filter)
        {
            _fs = filesystem;
            _filter = filter;
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return new ChangeTokenCallback();
        }

        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;


        private class ChangeTokenCallback : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private class SiteDirectoryContents : IDirectoryContents
    {
        private readonly IFileSystem _fileSystem;
        private readonly UPath _path;

        public SiteDirectoryContents(IFileSystem fileSystem, UPath path)
        {
            _fileSystem = fileSystem;
            _path = path;
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            foreach (var path in _fileSystem.EnumeratePaths(_path))
            {
                yield return new SiteFileInfo(_fileSystem, path);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Exists => _fileSystem.DirectoryExists(_path);
    }
}