// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Cards;

public abstract class CardsBase : DynamicObject<CardsPlugin>
{
    protected CardsBase(string name, CardsPlugin parent) : base(parent)
    {
        parent.SetValue(name, this, true);
    }

    public bool Enable
    {
        get => GetSafeValue<bool>("enable");
        set => SetValue("enable", value);
    }

    public string? Title
    {
        get => GetSafeValue<string>("title");
        set => SetValue("title", value);
    }

    public string? Description
    {
        get => GetSafeValue<string>("description");
        set => SetValue("description", value);
    }

    public string? Image
    {
        get => GetSafeValue<string>("image");
        set => SetValue("image", value);
    }

    public string? ImageAlt
    {
        get => GetSafeValue<string>("image_alt");
        set => SetValue("image_alt", value);
    }
}
