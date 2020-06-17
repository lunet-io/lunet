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

namespace Lunet.Bundles.SourceMaps
{
	/// <summary>
	/// Used to convert Base64 characters values into integers
	/// </summary>
	internal static class Base64Converter
	{
		private const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
		private static readonly Dictionary<char, int> _base64DecodeMap = new Dictionary<char, int>();

		static Base64Converter()
		{
			for (int i = 0; i < Base64Alphabet.Length; i += 1)
			{
				_base64DecodeMap[Base64Alphabet[i]] = i;
			}
		}

		/// <summary>
		/// Converts a base64 value to an integer.
		/// </summary>
		internal static int FromBase64(char base64Value)
		{
			int result;
			if (!_base64DecodeMap.TryGetValue(base64Value, out result))
			{
				throw new ArgumentOutOfRangeException(nameof(base64Value), "Tried to convert an invalid base64 value");
			}

			return result;
		}

		/// <summary>
		/// Converts a integer to base64 value
		/// </summary>
		internal static char ToBase64(int value)
		{
			if (value < 0 || value >= Base64Alphabet.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			return Base64Alphabet[value];
		}
	}
}
