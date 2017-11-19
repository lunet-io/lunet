// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Zio;

namespace Lunet.Datas
{
    public class DataObject : DynamicObject<DatasPlugin>
    {
        public DataObject(DatasPlugin parent) : base(parent)
        {
        }
    }

    public class DataFolderObject : DataObject
    {
        public DataFolderObject(DatasPlugin parent, DirectoryEntry folder) : base(parent)
        {
            Folder = folder;
        }

        public DirectoryEntry Folder { get; }

        public override string ToString()
        {
            return $"DataFolder({Folder.FullName})";
        }
    }
}