// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Core;
using Scriban.Runtime;
using Tomlyn;
using Tomlyn.Model;

namespace Lunet.Toml;

/// <summary>
/// Helper functions to decode a TOML into a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/>.
/// </summary>
public static class TomlUtil
{
    private static readonly object BoolTrue = true;
    private static readonly object BoolFalse = false;

    /// <summary>
    /// Extracts a TOML object from the TOML text.
    /// </summary>
    /// <param name="text">An input TOML text</param>
    /// <param name="tomlFile">The name of the TOML file.</param>
    /// <returns>The parsed TOML object to a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/></returns>
    public static object FromText(string text, string tomlFile = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var syntax = Tomlyn.Toml.Parse(text, tomlFile);
            var model = syntax.ToModel();
            return ConvertFromToml(model);
        }
        catch (Exception ex)
        {
            throw new LunetException($"Error while parsing {tomlFile}. {ex.Message}");
        }
    }

    private static object ConvertFromToml(object element)
    {
        switch (element)
        {
            case TomlArray tomlArray:
                var array = new ScriptArray();
                foreach (var value in tomlArray)
                {
                    array.Add(ConvertFromToml(value));
                }
                return array;

            case TomlTable tomlTable:
                var obj = new ScriptObject();
                foreach (var keyValue in tomlTable)
                {
                    obj.Add(keyValue.Key, ConvertFromToml(keyValue.Value));
                }
                return obj;

            case TomlTableArray tomlTableArray:
                var tableArray = new ScriptArray();
                foreach (var value in tomlTableArray)
                {
                    tableArray.Add(ConvertFromToml(value));
                }
                return tableArray;

            case TomlValue tomlValue:
                return tomlValue.ValueAsObject;

            case string str:
                return str;
            case int value:
                return value;
            case float valuef:
                return valuef;
            case double valued:
                return valued;
            case long valuel:
                return valuel;
            case DateTime datetime:
                return datetime;
            case bool bValue:
                return bValue ? BoolTrue : BoolFalse;

            case null:
                return null;
            default:
                throw new NotSupportedException($"The TomlObject {element?.GetType()} is not supported");
        }

        return null;
    }
}