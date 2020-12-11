// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Resources;
using Zio;

namespace Lunet.Search
{
    public class SearchPlugin : SitePlugin
    {
        public static readonly UPath DefaultUrl = UPath.Root / "js" / "lunet-search.db";

        public const string DefaultKind = SqliteSearchEngine.EngineName;

        public SearchPlugin(SiteObject site, BundlePlugin bundlePlugin, ResourcePlugin resourcePlugin) : base(site)
        {
            BundlePlugin = bundlePlugin;
            ResourcePlugin = resourcePlugin;
            Enable = false;
            Engine = DefaultKind;
            Url = (string)DefaultUrl;

            SearchEngines = new List<SearchEngine>()
            {
                new LunrSearchEngine(this),
                new SqliteSearchEngine(this)
            };

            Excludes = new PathCollection();
            SetValue("excludes", Excludes, true);

            site.SetValue("search", this, true);

            var processor = new SearchProcessorDispatch(this);
            site.Content.BeforeLoadingProcessors.Add(processor);
            // It is important to insert the processor at the beginning 
            // because we output values used by the BundlePlugin
            site.Content.BeforeProcessingProcessors.Insert(0, processor);
            site.Content.AfterRunningProcessors.Add(processor);
        }

        public bool Enable
        {
            get => GetSafeValue<bool>("enable");
            set => SetValue("enable", value);
        }

        internal BundlePlugin BundlePlugin { get; }

        internal ResourcePlugin ResourcePlugin { get; }

        public PathCollection Excludes { get; }

        public string Engine
        {
            get => GetSafeValue<string>("engine");
            set => SetValue("engine", value);
        }

        public List<SearchEngine> SearchEngines { get; }

        public bool Worker
        {
            get => GetSafeValue<bool>("worker");
            set => SetValue("worker", value);
        }

        public string Url
        {
            get => GetSafeValue<string>("url");
            set => SetValue("url", value);
        }

        private class SearchProcessorDispatch : ContentProcessor<SearchPlugin>
        {
            private SearchEngine _selectedEngine;

            public SearchProcessorDispatch(SearchPlugin plugin) : base(plugin)
            {
            }
            
            public override void Process(ProcessingStage stage)
            {
                if (stage == ProcessingStage.BeforeLoadingContent)
                {
                    if (!Plugin.Enable) return;

                    var engine = Plugin.Engine ?? DefaultKind;
                    foreach (var processor in Plugin.SearchEngines)
                    {
                        if (processor.Name == engine)
                        {
                            _selectedEngine = processor;
                            break;
                        }
                    }

                    // If we haven't found a processor, no need to continue
                    if (_selectedEngine == null)
                    {
                        Site.Error($"Unable to find search engine `{engine}`. Search is disabled");
                        return;
                    }

                    _selectedEngine.Process(stage);
                }
                else if (stage == ProcessingStage.BeforeProcessingContent)
                {
                    _selectedEngine?.Process(stage);
                }
            }

            public override ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage)
            {
                return _selectedEngine?.TryProcessContent(file, stage) ?? ContentResult.Continue;
            }
        }
    }
}