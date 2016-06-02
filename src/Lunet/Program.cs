﻿using System;
using Lunet.Core;
using Lunet.Logging;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        static int Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var site = new SiteObject(loggerFactory).AddConsoleLogger();
            return site.Run(args);
        }

        private static void DumpDependencies(SiteObject site, string type, PageCollection pages)
        {
            foreach (var page in pages)
            {
                if (page.Discard)
                {
                    continue;
                }

                Console.WriteLine($"Dependency {type} [{page.Path ?? page.Url}]");
                foreach (var dep in page.Dependencies)
                {
                    foreach (var file in dep.GetFiles())
                    {
                        Console.WriteLine($"    -> {file}");
                    }
                }
            }
        }
    }
}