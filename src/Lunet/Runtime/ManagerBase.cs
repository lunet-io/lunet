// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Runtime
{
    /// <summary>
    /// Base class for a Manager.
    /// </summary>
    /// <seealso cref="LunetObject" />
    public abstract class ManagerBase : LunetObject, ISiteable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerBase"/> class.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="site"/> is null</exception>
        protected ManagerBase(SiteObject site)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            Site = site;
        }

        /// <summary>
        /// Gets the site object.
        /// </summary>
        public SiteObject Site { get; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void InitializeBeforeConfig()
        {
        }

        public virtual void InitializeAfterConfig()
        {
        }
    }
}