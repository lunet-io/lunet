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

/* Based on the Base 64 VLQ implementation in Closure Compiler:
 * https://github.com/google/closure-compiler/blob/master/src/com/google/debugging/sourcemap/Base64VLQ.java
 *
 * Copyright 2011 The Closure Compiler Authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Collections.Generic;

namespace Lunet.Bundles.SourceMaps
{
	/// <summary>
	/// This class provides a mechanism for converting a Base64 Variable-length quantity (VLQ)
	/// into an array of integers.
	/// </summary>
	internal static class Base64VlqDecoder
	{
		/// <summary>
		/// Converts to a two-complement value from a value where the sign bit is
		/// is placed in the least significant bit.For example, as decimals:
		/// 2 (10 binary) becomes 1, 3 (11 binary) becomes -1
		/// 4 (100 binary) becomes 2, 5 (101 binary) becomes -2
		/// </summary>
		private static int FromVlqSigned(int value)
		{
			bool negate = (value & 1) == 1;
			value = value >> 1;
			return negate ? -value : value;
		}

		/// <summary>
		/// Returns a list of integers corresponding to an input string that is Base64 VLQ encoded
		/// </summary>
		internal static List<int> Decode(string input)
		{
			List<int> result = new List<int>();
			Base64CharProvider charProvider = new Base64CharProvider(input);

			while (!charProvider.IsEmpty())
			{
				result.Add(DecodeNextInteger(charProvider));
			}

			return result;
		}

		private class Base64CharProvider
		{
			private readonly string _backingString;
			private int _currentIndex = 0;

			public Base64CharProvider(string s)
			{
				_backingString = s;
			}

			internal char GetNextCharacter()
			{
				char nextChar = _backingString[_currentIndex];
				_currentIndex += 1;
				return nextChar;
			}

			/// <summary>
			/// Returns true when no more characters can be provided.
			/// </summary>
			internal bool IsEmpty()
			{
				return _currentIndex >= _backingString.Length;
			}
		}

		/// <summary>
		/// Reads characters from the Base64CharProvider until a complete integer value has been extracted.
		/// </summary>
		private static int DecodeNextInteger(Base64CharProvider charProvider)
		{
			int result = 0;
			bool continuation;
			int shift = 0;
			do
			{
				char c = charProvider.GetNextCharacter();
				int digit = Base64Converter.FromBase64(c);
				continuation = (digit & Base64VlqConstants.VlqContinuationBit) != 0;
				digit &= Base64VlqConstants.VlqBaseMask;
				result = result + (digit << shift);
				shift = shift + Base64VlqConstants.VlqBaseShift;
			} while (continuation);

			return FromVlqSigned(result);
		}
	}
}
