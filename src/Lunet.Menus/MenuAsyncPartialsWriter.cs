// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Concurrent;
using Lunet.Core;
using Zio;

namespace Lunet.Menus;

internal sealed class MenuAsyncPartialsWriter : ProcessorBase<MenuPlugin>
{
    public const string SiteKey = "__menu_async_partials";

    public MenuAsyncPartialsWriter(MenuPlugin plugin) : base(plugin)
    {
    }

    public override void Process(ProcessingStage stage)
    {
        if (stage != ProcessingStage.AfterProcessingContent)
        {
            return;
        }

        var partials = Site.GetSafeValue<ConcurrentDictionary<UPath, string>>(SiteKey);
        if (partials is null || partials.Count == 0)
        {
            return;
        }

        foreach (var pair in partials)
        {
            var outputPath = pair.Key;
            var html = pair.Value;

            var content = new DynamicContentObject(Site, outputPath.FullName);
            content.ContentType = ContentType.Html;
            content.Content = html;

            Site.Content.TryCopyContentToOutput(content, outputPath);
        }
    }
}
