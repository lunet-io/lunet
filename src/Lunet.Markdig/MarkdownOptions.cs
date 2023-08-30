// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Markdown;

public class MarkdownOptions : DynamicObject<MarkdownPlugin>
{
    public MarkdownOptions(MarkdownPlugin parent) : base(parent)
    {
    }
    
    public string Extensions
    {
        get => GetSafeValue<string>("extensions", "advanced");
        set => SetValue("extensions", value);
    }

    public string CssImageAttribute
    {
        get => GetSafeValue<string>("css_img_attr");
        set => SetValue("css_img_attr", value);
    }
}