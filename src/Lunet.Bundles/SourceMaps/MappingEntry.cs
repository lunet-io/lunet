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

using System;

namespace Lunet.Bundles.SourceMaps;

public class MappingEntry
{
    /// <summary>
    /// The location of the line of code in the transformed code
    /// </summary>
    public SourcePosition GeneratedSourcePosition;

    /// <summary>
    /// The location of the code in the original source code
    /// </summary>
    public SourcePosition OriginalSourcePosition;

    /// <summary>
    /// The original name of the code referenced by this mapping entry
    /// </summary>
    public string OriginalName;

    /// <summary>
    /// The name of the file that originally contained this code
    /// </summary>
    public string OriginalFileName;

    public MappingEntry Clone()
    {
        return new MappingEntry
        {
            GeneratedSourcePosition = this.GeneratedSourcePosition.Clone(),
            OriginalSourcePosition = this.OriginalSourcePosition.Clone(),
            OriginalFileName = this.OriginalFileName,
            OriginalName = this.OriginalName
        };
    }

    public Boolean IsValueEqual(MappingEntry anEntry)
    {
        return (
            this.OriginalName == anEntry.OriginalName &&
            this.OriginalFileName == anEntry.OriginalFileName &&
            this.GeneratedSourcePosition.CompareTo(anEntry.GeneratedSourcePosition) == 0 &&
            this.OriginalSourcePosition.CompareTo(anEntry.OriginalSourcePosition) == 0);
    }
}