// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;

namespace Lunet.Statistics;

public class ContentStat
{
    public ContentStat(ContentObject contentObject)
    {
        if (contentObject == null) throw new ArgumentNullException(nameof(contentObject));
        ContentObject = contentObject;
    }

    public ContentObject ContentObject { get; }

    public bool Static { get; set; }

    public long OutputBytes { get; set; }

    public TimeSpan LoadingParsingTime { get; set; }

    public TimeSpan LoadingTime { get; set; }

    public TimeSpan RunningTime { get; set; }

    public TimeSpan SummaryTime { get; set; }

    public TimeSpan OutputTime { get; set; }
}