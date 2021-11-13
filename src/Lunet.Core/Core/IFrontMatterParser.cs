// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Scriban.Parsing;

namespace Lunet.Core;

public interface IFrontMatterParser
{
    bool CanHandle(ReadOnlySpan<byte> header);

    bool CanHandle(ReadOnlySpan<char> header);

    IFrontMatter TryParse(string text, string sourceFilePath, out TextPosition position);
}