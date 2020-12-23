// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Scriban.Runtime;

namespace Lunet.Core
{
    /// <summary>
    /// Contains a map of layout type to a weight.
    /// Can be changed at site level.
    /// </summary>
    public class ContentLayoutTypes : DynamicObject
    {
        public const int SingleWeight = 0;

        public const int ListWeight = 10;
        
        public const string Single = "single";
        
        public const string List = "list";

        public ContentLayoutTypes()
        {
            this[Single] = SingleWeight;
            this[List] = ListWeight;
        }

        public void AddListType(string type)
        {
            this[type] = ListWeight;
        }
        
    }
}