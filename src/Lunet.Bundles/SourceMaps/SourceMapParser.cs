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

using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lunet.Bundles.SourceMaps;

public class SourceMapParser
{
    private readonly MappingsListParser _mappingsListParser;

    public SourceMapParser()
    {
        _mappingsListParser = new MappingsListParser();
    }

    /// <summary>
    /// Parses a stream representing a source map into a SourceMap object.
    /// </summary>
    public SourceMap? ParseSourceMap(StreamReader? sourceMapStream)
    {
        if (sourceMapStream == null)
        {
            return null;
        }
        using (JsonTextReader jsonTextReader = new JsonTextReader(sourceMapStream))
        {
            JsonSerializer serializer = new JsonSerializer();

            var result = serializer.Deserialize<SourceMap>(jsonTextReader);
            if (result == null)
            {
                return null;
            }
            result.ParsedMappings = _mappingsListParser.ParseMappings(result.Mappings ?? string.Empty, result.Names ?? new List<string>(), result.Sources ?? new List<string>());
            sourceMapStream.Close();
            return result;
        }
    }
}
