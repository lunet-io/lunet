using System;
using System.Reflection;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Logging;
using Lunet.Plugins.Bundles;
using Lunet.Extends;
using Lunet.Hosting;
using Lunet.Plugins.Layouts;
using Lunet.Plugins.Markdig;
using Lunet.Plugins.NUglify;
using Lunet.Plugins.SharpScss;
using Lunet.Plugins.Taxonomies;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        static int Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var site = new SiteObject(loggerFactory).AddConsoleLogger();

            site.Plugins.Factory.Add(() => new BundlePlugin());
            site.Plugins.Factory.Add(() => new ExtendPlugin());
            site.Plugins.Factory.Add(() => new LayoutPlugin());
            site.Plugins.Factory.Add(() => new HostingPlugin());
            site.Plugins.Factory.Add(() => new MarkdigPlugin());
            site.Plugins.Factory.Add(() => new NUglifyPlugin());
            site.Plugins.Factory.Add(() => new SharpScssPlugin());
            site.Plugins.Factory.Add(() => new TaxonomyPlugin());

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
