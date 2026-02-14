// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Core;

public class ExtraContent
{
    public string Uid { get; set; } = string.Empty;
        
    public string Name { get; set; } = string.Empty;

    public string DefinitionUid { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsExternal { get; set; }
}
