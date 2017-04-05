// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Autofac;
using Lunet.Core;
using Microsoft.Extensions.Logging;

namespace Lunet
{
    public class SiteFactory
    {
        private readonly ContainerBuilder _containerBuilder;

        public SiteFactory()
        {
            _containerBuilder = new ContainerBuilder();
            _containerBuilder.RegisterInstance(this);
        }

        public void Register<TPlugin>() where TPlugin : ISitePlugin
        {
            Register(typeof(TPlugin));
        }

        public void Register(Type pluginType)
        {
            if (pluginType == null) throw new ArgumentNullException(nameof(pluginType));
            if (!typeof(ISitePlugin).GetTypeInfo().IsAssignableFrom(pluginType))
            {
                throw new ArgumentException("Expecting a plugin type inheriting from ISitePlugin", nameof(pluginType));
            }
            _containerBuilder.RegisterType(pluginType).AsSelf().As<ISitePlugin>();
        }

        public SiteObject Build()
        {
            _containerBuilder.RegisterType<LoggerFactory>().As<ILoggerFactory>().SingleInstance();
            _containerBuilder.RegisterType<SiteObject>().SingleInstance();

            var container = _containerBuilder.Build();
            return container.Resolve<SiteObject>();
        }
    }
}