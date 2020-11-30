using System;
using System.Text.Json;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Json
{
    /// <summary>
    /// Helper functions to decode a JSON into a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/>.
    /// </summary>
    public static class JsonUtil
    {
        private static readonly object BoolTrue = true;
        private static readonly object BoolFalse = false;

        /// <summary>
        /// Extracts a JSON object from the JSON text.
        /// </summary>
        /// <param name="text">An input JSON text</param>
        /// <param name="jsonFile">The name of the JSON file.</param>
        /// <returns>The parsed JSON object to a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/></returns>
        public static object FromText(string text, string jsonFile = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                var jsonDoc = JsonDocument.Parse(text, new JsonDocumentOptions()
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                return ConvertFromJson(jsonDoc.RootElement);
            }
            catch (Exception ex)
            {
                throw new LunetException($"Error while parsing {jsonFile}. {ex.Message}");
            }
        }

        private static object ConvertFromJson(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new ScriptObject();
                    foreach (var prop in element.EnumerateObject())
                    {
                        obj[prop.Name] = ConvertFromJson(prop.Value);
                    }

                    return obj;
                case JsonValueKind.Array:
                    var array = new ScriptArray();
                    foreach (var nestedElement in element.EnumerateArray())
                    {
                        array.Add(ConvertFromJson(nestedElement));
                    }
                    return array;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                    {
                        return intValue;
                    }
                    else if (element.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    else if (element.TryGetUInt32(out var uintValue))
                    {
                        return uintValue;
                    }
                    else if (element.TryGetUInt64(out var ulongValue))
                    {
                        return ulongValue;
                    }
                    else if (element.TryGetDecimal(out var decimalValue))
                    {
                        return decimalValue;
                    }
                    else if (element.TryGetDouble(out var doubleValue))
                    {
                        return doubleValue;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unable to convert number {element}");
                    }
                case JsonValueKind.True:
                    return BoolTrue;
                case JsonValueKind.False:
                    return BoolFalse;
                default:
                    return null;
            }
        }
    }
}