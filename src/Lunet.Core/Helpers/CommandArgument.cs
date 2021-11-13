// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Lunet.Helpers;

public class CommandArgument
{
    public CommandArgument()
    {
        Values = new List<string>();
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; private set; }
    public bool MultipleValues { get; set; }
    public string Value
    {
        get
        {
            return Values.FirstOrDefault();
        }
    }
}