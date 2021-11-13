// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using Lunet.Core;

namespace Lunet.Summarizer;

public class SummarizerProcessor : ContentProcessor<SummarizerPlugin>
{
    public SummarizerProcessor(SummarizerPlugin plugin) : base(plugin)
    {
    }

    public override ContentResult TryProcessContent(ContentObject page, ContentProcessingStage stage)
    {
        Debug.Assert(stage == ContentProcessingStage.Processing);
        if (page.Content == null || page.ContentType != ContentType.Html)
        {
            return ContentResult.Continue;
        }
        SummarizerHelper.UpdateSummary(page);

        // Allow further processing of this page
        return ContentResult.Continue;
    }
}