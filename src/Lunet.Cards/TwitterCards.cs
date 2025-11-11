// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Cards;

public class TwitterCards(CardsPlugin parent) : CardsBase("twitter", parent)
{
    // <meta name="twitter:card" content="summary_large_image">
    public string Card
    {
        get => GetSafeValue<string>("card");
        set => SetValue("card", value);
    }

    // <meta name="twitter:site" content="@xoofx">
    public string User
    {
        get => GetSafeValue<string>("user");
        set => SetValue("user", value);
    }
}