// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Core
{
    public abstract class SiteModule
    {
        internal virtual void ConfigureInternal(SiteApplication application)
        {
            Configure(application);
        }

        protected abstract void Configure(SiteApplication application);
    }
    
    public abstract class SiteModule<TPlugin> : SiteModule where TPlugin : ISitePlugin
    {
        protected SiteModule()
        {
        }

        internal override void ConfigureInternal(SiteApplication application)
        {
            application.Config.RegisterPlugin<TPlugin>();
            base.ConfigureInternal(application);
        }

        protected override void Configure(SiteApplication application)
        {
        }
    }
}