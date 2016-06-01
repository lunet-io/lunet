// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

namespace Lunet.Core
{
    public enum ContentResult
    {
        /// <summary>
        /// The page was not processed by the <see cref="IContentProcessor.TryProcess"/>
        /// </summary>
        None,

        /// <summary>
        /// The page was processed by the <see cref="IContentProcessor.TryProcess"/> 
        /// and allow other processors to transform it.
        /// </summary>
        Continue,

        /// <summary>
        /// The page was processed by the <see cref="IContentProcessor.TryProcess"/> 
        /// but we break the any further processing of this page
        /// </summary>
        Break,
    }
}