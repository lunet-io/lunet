// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Plugins
{
    /// <summary>
    /// Attribute to setup in a plugin assembly to list the plugin types (and avoid having to scan all types)
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class SitePluginAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SitePluginAttribute"/> class.
        /// </summary>
        /// <param name="pluginType">Type of the plugin to instantiate. The type must inherit from <see cref="ISitePlugin"/>.</param>
        public SitePluginAttribute(Type pluginType)
        {
            PluginType = pluginType;
        }

        /// <summary>
        /// Gets the type of the plugin. The type must inherit from <see cref="ISitePlugin"/>
        /// </summary>
        public Type PluginType { get; }

        public int Order { get; set; }
    }
}