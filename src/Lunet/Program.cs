using System;
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

namespace Lunet
{
    class Program
    {
        static int Main(string[] args)
        {
            var siteFactory = new SiteFactory()
                .Register<BundlePlugin>()
                .Register<ExtendsPlugin>()
                .Register<LayoutPlugin>()
                .Register<ResourcePlugin>()
                .Register<DatasPlugin>()
                .Register<WatcherPlugin>()
                .Register<HostingPlugin>()
                .Register<MarkdownPlugin>()
                .Register<MinifierPlugin>()
                .Register<ScssPlugin>()
                .Register<TaxonomyPlugin>()
                .Register<YamlPlugin>();

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

                Console.WriteLine($"Dependency {type} [{page.Path.FullName ?? page.Url}]");
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
