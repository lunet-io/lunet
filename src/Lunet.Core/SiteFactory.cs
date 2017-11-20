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

            // Pre-register some type
            _containerBuilder.RegisterInstance(this);
            _containerBuilder.RegisterType<LoggerFactory>().As<ILoggerFactory>().SingleInstance();
            _containerBuilder.RegisterType<SiteObject>().SingleInstance();
        }

        public ContainerBuilder ContainerBuilder => _containerBuilder;

        public SiteFactory Register<TPlugin>() where TPlugin : ISitePlugin
        {
            Register(typeof(TPlugin));
            return this;
        }

        public SiteFactory Register(Type pluginType)
        {
            if (pluginType == null) throw new ArgumentNullException(nameof(pluginType));
            if (!typeof(ISitePlugin).GetTypeInfo().IsAssignableFrom(pluginType))
            {
                throw new ArgumentException("Expecting a plugin type inheriting from ISitePlugin", nameof(pluginType));
            }
            _containerBuilder.RegisterType(pluginType).SingleInstance().AsSelf().As<ISitePlugin>();
            return this;
        }

        public SiteObject Build()
        {
            var container = _containerBuilder.Build();
            return container.Resolve<SiteObject>();
        }
    }
}