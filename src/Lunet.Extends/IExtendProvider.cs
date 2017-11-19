// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Zio;

namespace Lunet.Extends
{
    public interface IExtendProvider
    {
        string Name { get; }

        IEnumerable<ExtendDescription> FindAll(SiteObject site);

        bool TryInstall(SiteObject site, string extend, string version, IFileSystem outputFileSystem);
    }
}