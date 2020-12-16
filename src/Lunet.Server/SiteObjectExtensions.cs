// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Server
{
    public static class SiteObjectExtensions
    {
        private const string LiveReload = "livereload";

        public static bool GetLiveReload(this SiteObject site)
        {
            return site.GetSafeValue<bool>(LiveReload);
        }

        public static void SetLiveReload(this SiteObject site, bool value)
        {
            site[LiveReload] = value;
        }
    }
}