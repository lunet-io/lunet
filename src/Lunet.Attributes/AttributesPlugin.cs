// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Attributes
{
    public class AttributesPlugin : SitePlugin
    {
        public AttributesPlugin(SiteObject site) : base(site)
        {
            var attributesObject = new AttributesObject();
            Site.Scripts.Builtins.SetValue("attributes", attributesObject, true);
            Site.Content.BeforeLoadingContentProcessors.Add(attributesObject.ProcessAttributesForPath);
        }
    }
}