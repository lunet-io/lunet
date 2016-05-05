// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
namespace Lunet.Extends
{
    public struct ExtendDescription
    {
        public ExtendDescription(string name, string description, string url, string directory) : this()
        {
            Name = name;
            Description = description;
            Url = url;
            Directory = directory;
        }

        public string Name { get; }

        public string Description { get;  }

        public string Url { get;  }

        public string Directory { get; }

        internal IExtendProvider Provider { get; set; }
    }
}