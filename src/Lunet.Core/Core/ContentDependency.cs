// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lunet.Helpers;
using Zio;

namespace Lunet.Core;

/// <summary>
/// Defines a dependency between content.
/// </summary>
public abstract class ContentDependency
{
    //public abstract IEnumerable<FileEntry> GetFiles();
}

[DebuggerDisplay("Page: {Page.Path}")]
public class PageContentDependency : ContentDependency
{
    public PageContentDependency(ContentObject page)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
    }

    public ContentObject Page { get; }

    // TODO: should we keep dependencies?

    //public override IEnumerable<FileEntry> GetFiles()
    //{
    //    if (!Page.SourceFile.IsEmpty)
    //    {
    //        yield return Page.SourceFile;
    //    }
    //    foreach (var dep in Page.Dependencies)
    //    {
    //        foreach (var file in dep.GetFiles())
    //        {
    //            yield return file;
    //        }
    //    }
    //}
}

[DebuggerDisplay("File: {File}")]
public class FileContentDependency : ContentDependency
{
    public FileContentDependency(FileEntry file)
    {
        File = file;
    }

    public FileEntry File { get; }

    //public override IEnumerable<FileEntry> GetFiles()
    //{
    //    yield return File;
    //}
}