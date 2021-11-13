// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Core;

public abstract class ContentProcessor<TPlugin> : ProcessorBase<TPlugin>, IContentProcessor where TPlugin : ISitePlugin
{
    protected ContentProcessor(TPlugin plugin) : base(plugin)
    {
    }

    public abstract ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage);
}