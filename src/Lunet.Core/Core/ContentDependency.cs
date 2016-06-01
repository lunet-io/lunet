// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lunet.Helpers;

namespace Lunet.Core
{
    /// <summary>
    /// Defines a dependency between content.
    /// </summary>
    public abstract class ContentDependency
    {
        public abstract IEnumerable<FileInfo> GetFiles();
    }

    [DebuggerDisplay("Page: {Page.Path}")]
    public class PageContentDependency : ContentDependency
    {
        public PageContentDependency(ContentObject page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            Page = page;
        }

        public ContentObject Page { get; }

        public override IEnumerable<FileInfo> GetFiles()
        {
            if (Page.SourceFileInfo != null)
            {
                yield return Page.SourceFileInfo;
            }
            foreach (var dep in Page.Dependencies)
            {
                foreach (var file in dep.GetFiles())
                {
                    yield return file;
                }
            }
        }
    }

    [DebuggerDisplay("File: {File}")]
    public class FileContentDependency : ContentDependency
    {
        public FileContentDependency(string file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            File = new FileInfo(file).Normalize();
        }


        public FileContentDependency(FileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            File = file.Normalize(); ;
        }

        public FileInfo File { get; }

        public override IEnumerable<FileInfo> GetFiles()
        {
            yield return File;
        }
    }
}