using System;
using Lunet.Attributes;
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
using Lunet.Rss;
using Lunet.Taxonomies;
using Lunet.Scss;
using Lunet.Search;
using Lunet.Server;
using Lunet.Sitemaps;
using Lunet.Summarizer;
using Lunet.Toml;
using Lunet.Watcher;
using Lunet.Yaml;

namespace Lunet
{
    class Program
    {
        static int Main(string[] args)
        {
            // The order they are registered here is important
            var site = new SiteObject()
                .Register<BundlePlugin>()
                .Register<MenuPlugin>()
                .Register<ExtendsPlugin>()
                .Register<SummarizerPlugin>()
                .Register<MarkdownPlugin>()
                .Register<LayoutPlugin>()
                .Register<ResourcePlugin>()
                .Register<DatasPlugin>()
                .Register<WatcherPlugin>()
                .Register<ServerPlugin>()
                .Register<MinifierPlugin>()
                .Register<RssPlugin>()
                .Register<ScssPlugin>()
                .Register<TaxonomyPlugin>()
                .Register<CardsPlugin>()
                .Register<SearchPlugin>()
                .Register<SitemapsPlugin>()
                .Register<AttributesPlugin>()
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
