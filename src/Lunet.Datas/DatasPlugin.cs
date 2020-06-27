// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Lunet.Helpers;

namespace Lunet.Datas
{
    public class DatasPlugin : SitePlugin
    {
        public DatasPlugin(SiteObject site) : base(site)
        {
            RootDataObject = new DataObject(this);
            DataLoaders = new OrderedList<IDataLoader>();

            Site.SetValue("data", RootDataObject, true);
            site.Content.BeforeInitializingProcessors.Add(new DatasProcessor(this));
        }

        public DataObject RootDataObject { get; }


        public OrderedList<IDataLoader> DataLoaders { get; }
    }
}