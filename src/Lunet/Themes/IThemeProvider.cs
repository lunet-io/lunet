// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Runtime;

namespace Lunet.Themes
{
    public interface IThemeProvider
    {
        string Name { get; }

        IEnumerable<ThemeDescription> FindAll(SiteObject site);

        bool TryInstall(SiteObject site, string theme, string outputPath);
    }
}