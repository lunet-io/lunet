// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using Lunet.Core;
using Zio;

namespace Lunet.Resources
{
    /// <summary>
    /// An object associated to a resource, accessible at runtime.
    /// </summary>
    /// <seealso cref="DynamicObject" />
    public class ResourceObject : DynamicObject
    {
        public ResourceObject(ResourceProvider provider, string name, string version, DirectoryEntry absoluteDirectory)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (absoluteDirectory == null) throw new ArgumentNullException(nameof(absoluteDirectory));

            Name = name;
            Version = version;
            AbsoluteDirectory = absoluteDirectory;
            Provider = provider;
            Path = absoluteDirectory.FullName;

            SetValue("provider", Provider.Name, true);
            SetValue("path", Path, true);
        }

        public string Name { get; }

        public string Version { get; }

        public ResourceProvider Provider { get; }

        public string Path { get; }

        public DirectoryEntry AbsoluteDirectory { get;  }
    }
}