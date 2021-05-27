// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Core
{
    public class ExtraContent
    {
        public string Uid { get; set; }
        
        public string Name { get; set; }

        public string DefinitionUid { get; set; }

        public string FullName { get; set; }

        public bool IsExternal { get; set; }
    }
}