// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Scriban.Runtime;
using Zio;

namespace Lunet.Api.DotNet
{
    public class ApiDotNetProject
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public ScriptObject Properties { get; set; }
        
        public UPath CachePath { get; set; }

        public ApitDotNetCacheState CacheState { get; set; }
        
        public ScriptObject Api { get; set; }
    }
    
    public enum ApitDotNetCacheState
    {
        Invalid,
        NotFound,
        Found,
        New,
    }
    
    
}