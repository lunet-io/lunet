// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Json;

/// <summary>
/// Helper functions to decode a JSON into a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/>.
/// </summary>
public static class JsonUtil
{
    private static readonly object BoolTrue = true;
    private static readonly object BoolFalse = false;

    private static readonly JsonDocumentOptions CommonOptions = new JsonDocumentOptions()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

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
            var jsonDoc = JsonDocument.Parse(text, CommonOptions);
            return ConvertFromJson(jsonDoc.RootElement, new StringCache());
        }
        catch (Exception ex)
        {
            throw new LunetException($"Error while parsing {jsonFile}. {ex.Message}");
        }
    }

    public static object FromStream(Stream stream, string jsonFile = null)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        try
        {
            var jsonDoc = JsonDocument.Parse(stream, CommonOptions);
            return ConvertFromJson(jsonDoc.RootElement, new StringCache());
        }
        catch (Exception ex)
        {
            throw new LunetException($"Error while parsing {jsonFile}. {ex.Message}");
        }
    }

    private static object ConvertFromJson(JsonElement element, StringCache cache)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new ScriptObject();
                foreach (var prop in element.EnumerateObject())
                {
                    obj[cache.Get(prop.Name)] = ConvertFromJson(prop.Value, cache);
                }

                return obj;
            case JsonValueKind.Array:
                var array = new ScriptArray();
                foreach (var nestedElement in element.EnumerateArray())
                {
                    array.Add(ConvertFromJson(nestedElement, cache));
                }
                return array;
            case JsonValueKind.String:
                return cache.Get(element.GetString());
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

    /// <summary>
    /// Allow to cache duplicated string when deserializing
    /// </summary>
    private class StringCache : Dictionary<string, string>
    {
        public string Get(string name)
        {
            if (name == null) return null;
            // Arbitrary limit, we don't need to cache everything but
            // optimize keys
            if (name.Length >= 512) return name;
            if (this.TryGetValue(name, out var cached))
            {
                return cached;
            }
            Add(name, name);
            return name;
        }
    }
}