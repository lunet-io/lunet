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
using System.Collections.Generic;
using System.Linq;

namespace Lunet.Bundles.SourceMaps
{
	public class SourceMap
	{
		/// <summary>
		/// The version of the source map specification being used
		/// </summary>
		public int Version;

		/// <summary>
		/// The name of the generated file to which this source map corresponds
		/// </summary>
		public string File;

		/// <summary>
		/// The raw, unparsed mappings entry of the soure map
		/// </summary>
		public string Mappings;

		/// <summary>
		/// The list of source files that were the inputs used to generate this output file
		/// </summary>
		public List<string> Sources;

		/// <summary>
		/// A list of known original names for entries in this file
		/// </summary>
		public List<string> Names;

		/// <summary>
		/// Parsed version of the mappings string that is used for getting original names and source positions
		/// </summary>
		public List<MappingEntry> ParsedMappings;

        public SourceMap Clone()
        {
            return new SourceMap
            {
                Version = this.Version,
                File = this.File,
                Mappings = this.Mappings,
                Sources = new List<string>(this.Sources),
                Names = new List<string>(this.Names),
                ParsedMappings = new List<MappingEntry>(this.ParsedMappings.Select(m => m.Clone()))
            };
        }

		/// <summary>
		/// Applies the mappings of a sub source map to the current source map
		/// Each mapping to the supplied source file is rewritten using the supplied source map
        /// This is useful in situations where we have a to b to c, with mappings ba.map and cb.map
        /// Calling cb.ApplySourceMap(ba) will return mappings from c to a (ca)
		/// <param name="submap">The submap to apply</param>
		/// <param name="sourceFile">The filename of the source file. If not specified, submap's File property will be used</param>
		/// <returns>A new source map</returns>
		/// </summary>
		public SourceMap ApplySourceMap(SourceMap submap, string sourceFile = null)
		{
			if (submap == null)
			{
				throw new ArgumentNullException(nameof(submap));
			}

			if (sourceFile == null)
			{
				if (submap.File == null)
				{
					throw new Exception("ApplySourceMap expects either the explicit source file to the map, or submap's 'file' property");
				}

				sourceFile = submap.File;
			}

			SourceMap newSourceMap = new SourceMap
			{
				File = this.File,
				Version = this.Version,
				Sources = new List<string>(),
				Names = new List<string>(),
				ParsedMappings = new List<MappingEntry>()
            };

			// transform mappings in this source map
			foreach (MappingEntry mappingEntry in this.ParsedMappings)
			{
				MappingEntry newMappingEntry = mappingEntry.Clone();

				if (mappingEntry.OriginalFileName == sourceFile && mappingEntry.OriginalSourcePosition != null)
				{
					MappingEntry correspondingSubMapMappingEntry = submap.GetMappingEntryForGeneratedSourcePosition(mappingEntry.OriginalSourcePosition);

					if (correspondingSubMapMappingEntry != null)
					{
						// Copy the mapping
						newMappingEntry = new MappingEntry
						{
							GeneratedSourcePosition = mappingEntry.GeneratedSourcePosition.Clone(),
							OriginalSourcePosition = correspondingSubMapMappingEntry.OriginalSourcePosition.Clone(),
							OriginalName = correspondingSubMapMappingEntry.OriginalName?? mappingEntry.OriginalName,
							OriginalFileName = correspondingSubMapMappingEntry.OriginalFileName?? mappingEntry.OriginalFileName
						};
					}
				}

				// Copy into "Sources" and "Names"
				string originalFileName = newMappingEntry.OriginalFileName;
				string originalName = newMappingEntry.OriginalName;

				if (originalFileName != null && !newSourceMap.Sources.Contains(originalFileName))
				{
                    newSourceMap.Sources.Add(originalFileName);
				}

				if (originalName != null && !newSourceMap.Names.Contains(originalName))
				{
                    newSourceMap.Names.Add(originalName);
				}

                newSourceMap.ParsedMappings.Add(newMappingEntry);
			};

			return newSourceMap;
		}

        /// <summary>
        /// Finds the mapping entry for the generated source position. If no exact match is found, it will attempt
        /// to return a nearby mapping that should map to the same piece of code.
        /// </summary>
        /// <param name="generatedSourcePosition">The location in generated code for which we want to discover a mapping entry</param>
        /// <returns>A mapping entry that is a close match for the desired generated code location</returns>
        public virtual MappingEntry GetMappingEntryForGeneratedSourcePosition(SourcePosition generatedSourcePosition)
        {
            if (ParsedMappings == null)
            {
                return null;
            }

            MappingEntry mappingEntryToFind = new MappingEntry
            {
                GeneratedSourcePosition = generatedSourcePosition
            };

            int index = ParsedMappings.BinarySearch(mappingEntryToFind,
                Comparer<MappingEntry>.Create((a, b) => a.GeneratedSourcePosition.CompareTo(b.GeneratedSourcePosition)));

            // If we didn't get an exact match, let's try to return the closest piece of code to the given line
            if (index < 0)
            {
                // The BinarySearch method returns the bitwise complement of the nearest element that is larger than the desired element when there isn't a match.
                // Based on tests with source maps generated with the Closure Compiler, we should consider the closest source position that is smaller than the target value when we don't have a match.
                if (~index - 1 >= 0 && ParsedMappings[~index - 1].GeneratedSourcePosition.IsEqualish(generatedSourcePosition))
                {
                    index = ~index - 1;
                }
            }

            return index >= 0 ? ParsedMappings[index] : null;
        }
	}
}
