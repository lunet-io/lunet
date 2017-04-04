// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Core
{
    public interface ISiteService
    {
    }

    /// <summary>
    /// Base class for a service.
    /// </summary>
    /// <seealso cref="DynamicObject" />
    public abstract class ServiceBase : DynamicObject, ISiteService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBase"/> class.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="site"/> is null</exception>
        protected ServiceBase(SiteObject site)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
        }

        /// <summary>
        /// Gets the site object.
        /// </summary>
        public SiteObject Site { get; }
    }
}