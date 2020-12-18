
using System;
using System.Runtime.CompilerServices;

// Only visible to SerializerGen to generate the JsonSerializer.generated.cs
[assembly: InternalsVisibleTo("Lunet.Api.DotNet.Extractor.CodeGen")]

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