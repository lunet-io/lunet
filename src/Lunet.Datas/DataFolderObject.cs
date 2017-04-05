// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;

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
        public DataFolderObject(DatasPlugin parent, FolderInfo folder) : base(parent)
        {
            Folder = folder;
        }

        public FolderInfo Folder { get; }

        public override string ToString()
        {
            return $"DataFolder({Folder.FullName})";
        }
    }
}