// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Only visible to SerializerGen to generate the JsonSerializer.generated.cs
[assembly: InternalsVisibleTo("Lunet.Api.DotNet.Extractor.CodeGen")]
[assembly: InternalsVisibleTo("Lunet.Tests")]

namespace Newtonsoft.Json
{
    internal class JsonPropertyAttribute : System.Attribute
    {
        public JsonPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    internal class JsonIgnoreAttribute : Attribute
    {

    }

    internal class JsonExtensionDataAttribute : Attribute
    {

    }

    internal class JsonConstructorAttribute : Attribute
    {

    }
}

namespace YamlDotNet.Serialization
{
    internal class YamlMemberAttribute : System.Attribute
    {
        public string Alias { get; set; }
    }

    internal class YamlIgnoreAttribute : Attribute
    {

    }
}
