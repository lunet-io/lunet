// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Lunet.Core;
using Lunet.Helpers;

namespace Lunet.Plugins
{
    /// <summary>
    /// Manages plugins of a site.
    /// </summary>
    public sealed class PluginService : ServiceBase
    {
        internal PluginService(SiteObject site) : base(site)
        {
            Factory = new OrderedList<Func<ISitePlugin>>();
            List = new PluginCollection<ISitePlugin>(Site);
            Site.SetValue(SiteVariables.Plugins, this, true);
        }

        public OrderedList<Func<ISitePlugin>> Factory { get; }

        public PluginCollection<ISitePlugin> List { get; }

        public void LoadPlugins()
        {
            foreach (var pluginFactory in Factory)
            {
                List.Add(pluginFactory());
            }
        }

        public void ImportPluginsFromAssembly(Assembly assembly)
        {
            var attributes = new List<SitePluginAttribute >(assembly.GetCustomAttributes<SitePluginAttribute>());
            attributes.Sort((left, right) => left.Order.CompareTo(right.Order));
            foreach (var pluginAttr in attributes)
            {
                var pluginType = pluginAttr.PluginType;
                if (!typeof (ISitePlugin).GetTypeInfo().IsAssignableFrom(pluginType))
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
    }
}