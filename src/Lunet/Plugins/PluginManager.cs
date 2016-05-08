// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Layouts;
using Scriban.Runtime;

namespace Lunet.Plugins
{
    /// <summary>
    /// Manages plugins of a site.
    /// </summary>
    /// <seealso cref="LunetObject" />
    public sealed class PluginManager : ManagerBase
    {
        private readonly HashSet<object> initializedExtensions;
        private readonly Stopwatch clock;

        internal PluginManager(SiteObject site) : base(site)
        {
            List = new OrderedList<ISitePlugin>();

            initializedExtensions = new HashSet<object>();
            Site.SetValue(SiteVariables.Plugins, this, true);
            clock = new Stopwatch();
        }

        public OrderedList<ISitePlugin> List { get; }

        public override void InitializeBeforeConfig()
        {
            // We import all plugins from this assembly by default
            ImportPluginsFromAssembly(typeof(PluginManager).GetTypeInfo().Assembly);

            // Initialize all pending plugins
            InitializeSiteExtensions(List);
        }

        public override void InitializeAfterConfig()
        {
            // Initialize all plugins added
            InitializeSiteExtensions(List);
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

        public void InitializeSiteExtensions<T>(List<T> extensions) where T : ISitePluginCore
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
                            clock.Start();
                            extension.Initialize(Site);
                            clock.Stop();

                            // Update statistics
                            Site.Statistics.GetPluginStat(extension).InitializeTime = clock.Elapsed;
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