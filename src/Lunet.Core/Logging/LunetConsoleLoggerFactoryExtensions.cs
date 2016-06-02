// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Lunet.Core;

namespace Lunet.Logging
{
    public static class ConsoleLoggerExtensions
    {
        /// <summary>
        /// Adds a console logger on the <see cref="SiteObject"/>.
        /// </summary>
        public static SiteObject AddConsoleLogger(this SiteObject site)
        {
            site.LoggerFactory.AddProvider(new LunetConsoleLoggerProvider(site.LogFilter, false));
            return site;
        }
   }
}