// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Scriban;

namespace Lunet.Core;

public interface IFrontMatter
{
    void Evaluate(TemplateContext context);
}