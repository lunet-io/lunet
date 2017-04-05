using System;
using System.Reflection;
using Autofac;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Datas;
using Lunet.Logging;
using Lunet.Extends;
using Lunet.Hosting;
using Lunet.Layouts;
using Lunet.Markdown;
using Lunet.Minifiers;
using Lunet.Resources;
using Lunet.Taxonomies;
using Lunet.Scss;
using Lunet.Yaml;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    class Program
    {
        static int Main(string[] args)
        {
            var siteFactory = new SiteFactory();

            siteFactory.Register<BundlePlugin>();
            siteFactory.Register<ExtendsPlugin>();
            siteFactory.Register<LayoutPlugin>();
            siteFactory.Register<ResourcePlugin>();
            siteFactory.Register<DatasPlugin>();
            siteFactory.Register<WatcherPlugin>();
            siteFactory.Register<HostingPlugin>();
            siteFactory.Register<MarkdownPlugin>();
            siteFactory.Register<MinifierPlugin>();
            siteFactory.Register<ScssPlugin>();
            siteFactory.Register<TaxonomyPlugin>();
            siteFactory.Register<YamlPlugin>();

            var site = siteFactory.Build();

            site.AddConsoleLogger();

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
