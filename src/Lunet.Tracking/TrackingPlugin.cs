// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Tracking
{
    public class TrackingModule : SiteModule<AnalyticsPlugin>
    {
    }

    public class AnalyticsPlugin : SitePlugin
    {
        public AnalyticsPlugin(SiteObject site) : base(site)
        {
            Site.SetValue("tracking", new AnalyticsObject(this), true);
        }
    }

    public class AnalyticsObject : DynamicObject<AnalyticsPlugin>
    {
        public AnalyticsObject(AnalyticsPlugin parent) : base(parent)
        {
            this.SetValue("google", new GoogleAnalytics(this), true);
        }
    }

    public class GoogleAnalytics : DynamicObject<AnalyticsObject>
    {
        public GoogleAnalytics(AnalyticsObject parent) : base(parent)
        {
            Parent.Parent.Site.Html.Head.Includes.Add("_builtins/google-analytics.sbn-html");
        }
    }
}
