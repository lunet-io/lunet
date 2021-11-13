// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Lunet.Helpers;

// Stupid-simple console manager
public class Reporter
{
    private static readonly Reporter NullReporter = new Reporter(console: null);
    private static object _lock = new object();

    private readonly AnsiConsole _console;

    static Reporter()
    {
        Reset();
    }

    private Reporter(AnsiConsole console)
    {
        _console = console;
    }

    public static Reporter Output { get; private set; }
    public static Reporter Error { get; private set; }
    public static Reporter Verbose { get; private set; }

    /// <summary>
    /// Resets the Reporters to write to the current Console Out/Error.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            Output = new Reporter(AnsiConsole.GetOutput());
            Error = new Reporter(AnsiConsole.GetError());
        }
    }

    public void WriteLine(string message)
    {
        lock (_lock)
        {
            //if (CommandContext.ShouldPassAnsiCodesThrough())
            //{
            //    _console?.Writer?.WriteLine(message);
            //}
            //else
            {
                _console?.WriteLine(message);
            }
        }
    }

    public void WriteLine()
    {
        lock (_lock)
        {
            _console?.Writer?.WriteLine();
        }
    }

    public void Write(string message)
    {
        lock (_lock)
        {
            //if (CommandContext.ShouldPassAnsiCodesThrough())
            //{
            //    _console?.Writer?.Write(message);
            //}
            //else
            {
                _console?.Write(message);
            }
        }
    }
}