// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using DotNet.Globbing;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core;

public class GlobCollection : ScriptArray<Glob>
{
    public GlobCollection()
    {
        this.Import("clear", (Action)Clear);
        this.Import("add", (Action<string>)Add);
    }

    public void Add(string glob)
    {
        if (glob == null) throw new ArgumentNullException(nameof(glob));
        try
        {
            Add(Glob.Parse(glob));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid glob `{glob}` pattern. {ex.Message}", nameof(glob));
        }
    }

    public bool IsMatch(UPath path)
    {
        // Workaround for issue https://github.com/dazinator/DotNet.Glob/issues/82
        if (path == UPath.Root) return false;

        foreach (var glob in this)
        {
            if (glob.IsMatch(path.FullName))
            {
                return true;
            }
        }

        return false;
    }
}