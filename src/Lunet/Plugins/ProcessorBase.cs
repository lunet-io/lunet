using System;
using Lunet.Runtime;
using Scriban;

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