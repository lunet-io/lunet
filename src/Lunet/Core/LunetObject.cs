// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
namespace Lunet.Core
{
    /// <summary>
    /// Base class for an lunet object that provides a dynamic object
    /// accessible from scripts.
    /// </summary>
    public abstract class LunetObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LunetObject"/> class.
        /// </summary>
        protected LunetObject()
        {
            DynamicObject = new DynamicObject();
        }

        /// <summary>
        /// Gets the dynamic object attached to this instance.
        /// </summary>
        public IDynamicObject DynamicObject { get; }
    }
}