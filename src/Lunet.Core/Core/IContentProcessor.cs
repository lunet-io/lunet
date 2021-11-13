// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Scriban.Runtime;
using Zio;

namespace Lunet.Core;

public interface IContentProcessor : ISiteProcessor
{
    ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage);
}

public delegate void TryProcessPreContentDelegate(UPath path, ref ScriptObject preContent);