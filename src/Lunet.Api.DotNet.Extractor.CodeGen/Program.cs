using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Lunet.Api.DotNet.Extractor;
using Newtonsoft.Json;
using Scriban;

namespace Lunet.Api.DotNet.Extractor.CodeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = typeof(ExtractorAnalyzer).Assembly;

            var serializers = new Dictionary<string, List<(string, string)>>();

            foreach (var type in assembly.GetTypes())
            {
                var members = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var prop in members)
                {
                    var jsonAttr = prop.GetCustomAttribute<JsonPropertyAttribute>();
                    var ignore = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                    if (jsonAttr == null || ignore != null) continue;
                    var fullName = type.FullName.Replace('+', '.');

                    if (!serializers.TryGetValue(fullName, out var props))
                    {
                        props = new List<(string, string)>();

                        serializers.Add(fullName, props);
                    }

                    props.Add((jsonAttr.Name, prop.Name));
                }
            }


            var templateText = @"
using System;
namespace Lunet.Api.DotNet.Extractor
{
    public partial class JsonSerializer 
    {
        static JsonSerializer() 
        {
            {{~ for type in types ~}}
            _serializers.Add(typeof({{type.key}}), Serialize{{ type.key | string.replace '.' '_'}});
            {{~ end ~}}
        }
        {{~ for type in types ~}}
        private static void Serialize{{ type.key | string.replace '.' '_' }}(JsonSerializer serializer, object valueObj)
        {
            var value = ({{type.key}})valueObj;
            serializer.StartObject();
            {{~ for member in type.value ~}}
            serializer.WriteJsonKeyValue(""{{member.item1}}"", value.{{member.item2}});
            {{~ end ~}}
            serializer.EndObject();
        }
        {{~ end ~}}
    }
}
";
            var template = Template.Parse(templateText);
            var result = template.Render(new {  types = serializers });

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Lunet.Api.DotNet.Extractor", "JsonSerializer.generated.cs"), result, new UTF8Encoding(false, false));
        }
    }
}
