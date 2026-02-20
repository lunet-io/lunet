// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Markdown;

public class MarkdownOptions : DynamicObject<MarkdownPlugin>
{
    public MarkdownOptions(MarkdownPlugin parent) : base(parent)
    {
        AutoIdKind = "github";
    }
    
    public string Extensions
    {
        get => GetSafeValue<string>("extensions", "advanced") ?? "advanced";
        set => SetValue("extensions", value);
    }

    public string? CssImageAttribute
    {
        get => GetSafeValue<string>("css_img_attr");
        set => SetValue("css_img_attr", value);
    }

    public string? AutoIdKind
    {
        get => GetSafeValue<string>("auto_id_kind");
        set => SetValue("auto_id_kind", value);
    }
}
