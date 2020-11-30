using System;
using Lunet.Bundles;
using Lunet.Cards;
using Lunet.Core;
using Lunet.Datas;
using Lunet.Extends;
using Lunet.Json;
using Lunet.Layouts;
using Lunet.Markdown;
using Lunet.Menus;
using Lunet.Minifiers;
using Lunet.Resources;
using Lunet.Taxonomies;
using Lunet.Scss;
using Lunet.Search;
using Lunet.Server;
using Lunet.Sitemaps;
using Lunet.Toml;
using Lunet.Watcher;
using Lunet.Yaml;

namespace Lunet
{
    class Program
    {
        static int Main(string[] args)
        {
            var site = new SiteObject()
                .Register<BundlePlugin>()
                .Register<MenuPlugin>()
                .Register<ExtendsPlugin>()
                .Register<MarkdownPlugin>()
                .Register<LayoutPlugin>()
                .Register<ResourcePlugin>()
                .Register<DatasPlugin>()
                .Register<WatcherPlugin>()
                .Register<ServerPlugin>()
                .Register<MinifierPlugin>()
                .Register<ScssPlugin>()
                .Register<TaxonomyPlugin>()
                .Register<CardsPlugin>()
                .Register<SearchPlugin>()
                .Register<SitemapsPlugin>()
                .Register<YamlPlugin>()
                .Register<JsonPlugin>()
                .Register<TomlPlugin>();

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
