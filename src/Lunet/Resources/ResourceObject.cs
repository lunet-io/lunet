// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.
using System;
using Lunet.Runtime;

namespace Lunet.Resources
{
    /// <summary>
    /// An object associated to a resource, accessible at runtime.
    /// </summary>
    /// <seealso cref="Lunet.Runtime.LunetObject" />
    public class ResourceObject : LunetObject
    {
        public ResourceObject(ResourceProvider provider, string name, string version, string absolutePath)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (absolutePath == null) throw new ArgumentNullException(nameof(absolutePath));

            Name = name;
            Version = version;
            AbsolutePath = absolutePath;
            Provider = provider;
            Path = provider.Manager.Site.GetRelativePath(AbsolutePath);

            DynamicObject.SetValue("provider", Provider.Name, true);
            DynamicObject.SetValue("path", Path, true);
        }

        public string Name { get; }

        public string Version { get; }

        public ResourceProvider Provider { get; }

        public string Path { get; }

        public string AbsolutePath { get;  }
    }
}