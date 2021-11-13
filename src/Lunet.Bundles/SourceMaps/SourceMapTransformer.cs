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
using System.Collections.Generic;

namespace Lunet.Bundles.SourceMaps;

public static class SourceMapTransformer
{
    /// <summary>
    /// Removes column information from a source map
    /// This can significantly reduce the size of source maps
    /// If there is a tie between mapping entries, the first generated line takes priority
    /// <returns>A new source map</returns>
    /// </summary>
    public static SourceMap Flatten(SourceMap sourceMap)
    {
        SourceMap newMap = new SourceMap
        {
            File = sourceMap.File,
            Version = sourceMap.Version,
            Mappings = sourceMap.Mappings,
            Sources = sourceMap.Sources == null ? null : new List<string>(sourceMap.Sources),
            Names = sourceMap.Names == null ? null : new List<string>(sourceMap.Names),
            ParsedMappings = new List<MappingEntry>()
        };

        HashSet<int> visitedLines = new HashSet<int>();

        foreach (MappingEntry mapping in sourceMap.ParsedMappings)
        {
            int generatedLine = mapping.GeneratedSourcePosition.ZeroBasedLineNumber;

            if (!visitedLines.Contains(generatedLine))
            {
                visitedLines.Add(generatedLine);
                var newMapping = mapping.Clone();
                newMapping.GeneratedSourcePosition.ZeroBasedColumnNumber = 0;
                newMapping.OriginalSourcePosition.ZeroBasedColumnNumber = 0;
                newMap.ParsedMappings.Add(newMapping);
            }
        }

        return newMap;
    }
}