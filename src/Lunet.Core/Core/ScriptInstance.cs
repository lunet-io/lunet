// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Scriban.Syntax;

namespace Lunet.Scripts;

public class ScriptInstance
{
    public ScriptInstance(bool hasErrors, string sourceFilePath, IFrontMatter frontMatter, ScriptPage template)
    {
        HasErrors = hasErrors;
        SourceFilePath = sourceFilePath;
        FrontMatter = frontMatter;
        Template = template;
    }

    public readonly bool HasErrors;

    public readonly string SourceFilePath;

    public readonly IFrontMatter FrontMatter;

    public readonly ScriptPage Template;
}