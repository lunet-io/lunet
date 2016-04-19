// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using Lunet.Core;

namespace Lunet.Plugins
{
    public abstract class ProcessorBase : ISiteProcessor
    {
        protected SiteObject Site { get; private set; }

        public virtual string Name => GetType().Name;

        public void Initialize(SiteObject site)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            Site = site;
            InitializeCore();
        }

        protected virtual void InitializeCore()
        {
        }

        public virtual void BeginProcess()
        {
        }

        public virtual void EndProcess()
        {
        }
    }
}