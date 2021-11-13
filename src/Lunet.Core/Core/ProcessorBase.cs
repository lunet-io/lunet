// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Core;

public abstract class ProcessorBase<TPlugin> : SitePluginCore, ISiteProcessor where TPlugin : ISitePlugin
{
    protected ProcessorBase(TPlugin plugin) : base(GetSafePlugin(plugin).Site)
    {
        Plugin = plugin;
    }

    public TPlugin Plugin { get; }

    public virtual void Process(ProcessingStage stage)
    {
    }

    private static TPlugin GetSafePlugin(TPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        return plugin;
    }
}