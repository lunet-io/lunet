// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Core;

/// <summary>
/// Main interface for a pluggable processor on a <see cref="SiteObject"/>.
/// </summary>
public interface ISiteProcessor : ISitePluginCore
{
    void Process(ProcessingStage stage);
}