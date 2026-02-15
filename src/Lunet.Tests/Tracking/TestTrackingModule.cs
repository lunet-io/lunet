// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Tests.Infrastructure;
using Lunet.Tracking;

namespace Lunet.Tests.Tracking;

public class TestTrackingModule
{
    [Test]
    public void TestAnalyticsPluginRegistersTrackingObjectAndGoogleSection()
    {
        using var context = new SiteTestContext();
        _ = new AnalyticsPlugin(context.Site);

        var tracking = context.Site.GetSafeValue<AnalyticsObject>("tracking");
        Assert.NotNull(tracking);

        var google = tracking!.GetSafeValue<GoogleAnalytics>("google");
        Assert.NotNull(google);
        CollectionAssert.Contains(context.Site.Html.Head.Includes, "_builtins/google-analytics.sbn-html");
    }
}
