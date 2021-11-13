// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Lunet.Core;

namespace Lunet.Layouts;

public interface ILayoutConverter
{
    /// <summary>
    /// Gets a value indicating whether this converter should convert its input to its default output even if no layout is defined.
    /// </summary>
    /// <value><c>true</c> if this converter should convert its input to its default output even if no layout is defined.; otherwise, <c>false</c>.</value>
    bool ShouldConvertIfNoLayout { get; }

    void Convert(ContentObject page);
}