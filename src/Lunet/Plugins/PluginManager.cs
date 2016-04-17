// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lunet.Helpers;
using Lunet.Layouts;
using Lunet.Runtime;
using Textamina.Scriban.Runtime;

namespace Lunet.Plugins
{
    /// <summary>
    /// Manages plugins of a site.
    /// </summary>
    /// <seealso cref="LunetObject" />
    public sealed class PluginManager : ManagerBase
    {
        private readonly HashSet<object> initializedExtensions;

        internal PluginManager(SiteObject site) : base(site)
        {
            List = new OrderedList<ISitePlugin>();
            Processors = new OrderedList<ISiteProcessor>()
            {
                new LayoutProcessor() // Default processor
            };
            initializedExtensions = new HashSet<object>();
            Site.SetValue(SiteVariables.Plugins, this, true);
        }

        public OrderedList<ISitePlugin> List { get; }

        public OrderedList<ISiteProcessor> Processors { get; }

        public override void InitializeBeforeConfig()
        {
            // We import all plugins from this assembly by default
            ImportPluginsFromAssembly(typeof(PluginManager).GetTypeInfo().Assembly);

            // Initialize all pending plugins
            InitializePendingPluginsAndProcessors();
        }

        public override void InitializeAfterConfig()
        {
            // Initialize all plugins added
            InitializePendingPluginsAndProcessors();
        }

        public void ImportPluginsFromAssembly(Assembly assembly)
        {
            var attributes = new List<SitePluginAttribute >(assembly.GetCustomAttributes<SitePluginAttribute>());
            attributes.Sort((left, right) => left.Order.CompareTo(right.Order));
            foreach (var pluginAttr in attributes)
            {
                var pluginType = pluginAttr.PluginType;
                if (!typeof (ISitePlugin).IsAssignableFrom(pluginType))
                {
                    Site.Error($"The plugin [{pluginType}] must be of the type {typeof(ISitePlugin)}");
                    continue;
                }

                ISitePlugin pluginInstance;
                try
                {
                    pluginInstance = (ISitePlugin)Activator.CreateInstance(pluginType);
                }
                catch (Exception ex)
                {
                    Site.Error($"Unable to instantiate the plugin [{pluginType}]. Reason:{ex.GetReason()}");
                    continue;
                }

                List.Add(pluginInstance);
            }
        }


        public void BeginProcess()
        {
            // Callback plugins once files have been initialized but not yet processed
            foreach (var processors in Processors)
            {
                processors.BeginProcess();
            }
        }

        public void ProcessPages(List<ContentObject> pages)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));
            // Process pages
            var pageProcessors = Processors.OfType<IPageProcessor>().ToList();
            var pendingPageProcessors = new List<IPageProcessor>();
            foreach (var page in pages)
            {
                // If there is a script on this page, and it didn't run well, proceed to next page
                if (page.Script != null && !TryEvaluate(page))
                {
                    continue;
                }

                // By default working on all processors
                // Order is important!
                pendingPageProcessors.AddRange(pageProcessors);
                bool hasBeenProcessed = true;
                bool breakProcessing = false;

                // We process the page going through all IPageProcessor from the end of the list
                // (more priority) to the begining of the list (less priority).
                // An IPageProcessor can transform the page to another type of content
                // that could then be processed by another IPageProcessor
                // But we make sure that a processor cannot process a page more than one time
                // to avoid an infinite loop
                while (hasBeenProcessed && !breakProcessing)
                {
                    hasBeenProcessed = false;
                    for (int i = pendingPageProcessors.Count - 1; i >= 0; i--)
                    {
                        var processor = pendingPageProcessors[i];

                        // Note that page.ContentExtension can be changed by a processor 
                        // while processing a page
                        var result = processor.TryProcess(page);
                        if (result != PageProcessResult.None)
                        {
                            hasBeenProcessed = true;
                            pendingPageProcessors.RemoveAt(i);
                            breakProcessing = result == PageProcessResult.Break;
                            break;
                        }
                    }
                }
                pendingPageProcessors.Clear();

                // Copy only if the file are marked as include
                if (!breakProcessing && !page.Discard)
                {
                    Site.Generator.TryCopyContentToOutput(page, page.GetDestinationPath());
                }
            }
        }

        public void EndProcess()
        {
            // Callback plugins once files have been initialized but not yet processed
            foreach (var plugin in Processors)
            {
                plugin.EndProcess();
            }
        }

        private bool TryEvaluate(ContentObject page)
        {
            if (page.ScriptObject == null)
            {
                page.ScriptObject = new ScriptObject();
            }

            return Site.Scripts.TryEvaluate(page, page.Script, page.SourceFile, page.ScriptObject);
        }

        private void InitializePendingPluginsAndProcessors()
        {
            InitializeSiteExtensions(List);
            InitializeSiteExtensions(Processors);
        }

        private void InitializeSiteExtensions<T>(List<T> extensions) where T : ISitePluginCore
        {
            // Because we expect that a Processor they modify the list of processors (add)
            // We iterate until all processors have been initialized
            var hasInitialized = true;
            while (hasInitialized)
            {
                hasInitialized = false;
                var tempExtensions = new List<T>(extensions);
                foreach (var extension in tempExtensions)
                {
                    if (!initializedExtensions.Contains(extension))
                    {
                        hasInitialized = true;
                        try
                        {
                            extension.Initialize(Site);
                        }
                        catch (Exception ex)
                        {
                            Site.Error($"Error while initializing [{extension.Name}] from the type [{extension.GetType()}]. Reason:{ex.GetReason()}");
                            continue;
                        }
                        initializedExtensions.Add(extension);
                    }
                }
            }
        }
    }
}