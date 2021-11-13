﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Helpers;

public class CommandParsingException : Exception
{
    public CommandParsingException(CommandLineApplication command, string message)
        : base(message)
    {
        Command = command;
    }

    public CommandLineApplication Command { get; }
}