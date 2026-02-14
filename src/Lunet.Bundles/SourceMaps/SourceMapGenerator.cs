// Fork from https://github.com/Microsoft/sourcemap-toolkit
// Copyright (c) Microsoft Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR 
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lunet.Bundles.SourceMaps;

/// <summary>
/// Class to track the internal state during source map serialize
/// </summary>
internal class MappingGenerateState
{
    /// <summary>
    /// Last location of the code in the transformed code
    /// </summary>
    public readonly SourcePosition LastGeneratedPosition = new SourcePosition();

    /// <summary>
    /// Last location of the code in the source code
    /// </summary>
    public readonly SourcePosition LastOriginalPosition = new SourcePosition();

    /// <summary>
    /// List that contains the symbol names
    /// </summary>
    public readonly IList<string> Names;

    /// <summary>
    /// List that contains the file sources
    /// </summary>
    public readonly IList<string> Sources;

    /// <summary>
    /// Index of last file source
    /// </summary>
    public int LastSourceIndex { get; set; }

    /// <summary>
    /// Index of last symbol name
    /// </summary>
    public int LastNameIndex { get; set; }

    /// <summary>
    /// Whether this is the first segment in current line
    /// </summary>
    public bool IsFirstSegment { get; set; }

    public MappingGenerateState(IList<string> names, IList<string> sources)
    {
        Names = names;
        Sources = sources;
        IsFirstSegment = true;
    }
}

public class SourceMapGenerator
{
    /// <summary>
    /// Convenience wrapper around SerializeMapping, but returns a base 64 encoded string instead
    /// </summary>
    public string GenerateSourceMapInlineComment(SourceMap sourceMap, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        string mappings = SerializeMapping(sourceMap, jsonSerializerSettings);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(mappings);
        var encoded = Convert.ToBase64String(bytes);

        return @"//# sourceMappingURL=data:application/json;base64," + encoded;
    }


    /// <summary>
    /// Serialize SourceMap object to json string with given serialize settings
    /// </summary>
    public string SerializeMapping(SourceMap sourceMap, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        if (sourceMap == null)
        {
            throw new ArgumentNullException(nameof(sourceMap));
        }

        SourceMap mapToSerialize = new SourceMap()
        {
            File = sourceMap.File,
            Names = sourceMap.Names,
            Sources = sourceMap.Sources,
            Version = sourceMap.Version,
        };

        if (sourceMap.ParsedMappings != null && sourceMap.ParsedMappings.Count > 0)
        {
            MappingGenerateState state = new MappingGenerateState(sourceMap.Names ?? new List<string>(), sourceMap.Sources ?? new List<string>());
            List<char> output = new List<char>();

            foreach (MappingEntry entry in sourceMap.ParsedMappings)
            {
                SerializeMappingEntry(entry, state, output);
            }

            output.Add(';');

            mapToSerialize.Mappings = new string(output.ToArray());
        }

        return JsonConvert.SerializeObject(mapToSerialize,
            jsonSerializerSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            });
    }

    /// <summary>
    /// Convert each mapping entry to VLQ encoded segments
    /// </summary>
    internal void SerializeMappingEntry(MappingEntry entry, MappingGenerateState state, ICollection<char> output)
    {
        // Each line of generated code is separated using semicolons
        while (entry.GeneratedSourcePosition.ZeroBasedLineNumber != state.LastGeneratedPosition.ZeroBasedLineNumber)
        {
            state.LastGeneratedPosition.ZeroBasedColumnNumber = 0;
            state.LastGeneratedPosition.ZeroBasedLineNumber++;
            state.IsFirstSegment = true;
            output.Add(';');
        }

        // The V3 source map format calls for all Base64 VLQ segments to be seperated by commas.
        if (!state.IsFirstSegment)
            output.Add(',');

        state.IsFirstSegment = false;

        /*
         *	The following description was taken from the Sourcemap V3 spec https://docs.google.com/document/d/1U1RGAehQwRypUTovF1KRlpiOFze0b-_2gc6fAH0KY0k/mobilebasic?pref=2&pli=1
         *  The Sourcemap V3 spec is under a Creative Commons Attribution-ShareAlike 3.0 Unported License. https://creativecommons.org/licenses/by-sa/3.0/
         *  
         *  Each VLQ segment has 1, 4, or 5 variable length fields.
         *  The fields in each segment are:
         *  1. The zero-based starting column of the line in the generated code that the segment represents. 
         *     If this is the first field of the first segment, or the first segment following a new generated line(“;”), 
         *     then this field holds the whole base 64 VLQ.Otherwise, this field contains a base 64 VLQ that is relative to
         *     the previous occurrence of this field.Note that this is different than the fields below because the previous 
         *     value is reset after every generated line.
         *  2. If present, an zero - based index into the “sources” list.This field is a base 64 VLQ relative to the previous
         *     occurrence of this field, unless this is the first occurrence of this field, in which case the whole value is represented.
         *  3. If present, the zero-based starting line in the original source represented. This field is a base 64 VLQ relative to the
         *     previous occurrence of this field, unless this is the first occurrence of this field, in which case the whole value is 
         *     represented.Always present if there is a source field.
         *  4. If present, the zero - based starting column of the line in the source represented.This field is a base 64 VLQ relative to 
         *     the previous occurrence of this field, unless this is the first occurrence of this field, in which case the whole value is
         *     represented.Always present if there is a source field.
         *  5. If present, the zero - based index into the “names” list associated with this segment.This field is a base 64 VLQ relative 
         *     to the previous occurrence of this field, unless this is the first occurrence of this field, in which case the whole value
         *     is represented.
         */

        Base64VlqEncoder.Encode(output, entry.GeneratedSourcePosition.ZeroBasedColumnNumber - state.LastGeneratedPosition.ZeroBasedColumnNumber);
        state.LastGeneratedPosition.ZeroBasedColumnNumber = entry.GeneratedSourcePosition.ZeroBasedColumnNumber;

        if (entry.OriginalFileName != null && entry.OriginalSourcePosition != null)
        {
            int sourceIndex = state.Sources.IndexOf(entry.OriginalFileName);
            if (sourceIndex < 0)
            {
                throw new SerializationException("Source map contains original source that cannot be found in provided sources array");
            }

            Base64VlqEncoder.Encode(output, sourceIndex - state.LastSourceIndex);
            state.LastSourceIndex = sourceIndex;

            Base64VlqEncoder.Encode(output, entry.OriginalSourcePosition.ZeroBasedLineNumber - state.LastOriginalPosition.ZeroBasedLineNumber);
            state.LastOriginalPosition.ZeroBasedLineNumber = entry.OriginalSourcePosition.ZeroBasedLineNumber;

            Base64VlqEncoder.Encode(output, entry.OriginalSourcePosition.ZeroBasedColumnNumber - state.LastOriginalPosition.ZeroBasedColumnNumber);
            state.LastOriginalPosition.ZeroBasedColumnNumber = entry.OriginalSourcePosition.ZeroBasedColumnNumber;

            if (entry.OriginalName != null)
            {
                int nameIndex = state.Names.IndexOf(entry.OriginalName);
                if (nameIndex < 0)
                {
                    throw new SerializationException("Source map contains original name that cannot be found in provided names array");
                }

                Base64VlqEncoder.Encode(output, nameIndex - state.LastNameIndex);
                state.LastNameIndex = nameIndex;
            }
        }
    }
}
