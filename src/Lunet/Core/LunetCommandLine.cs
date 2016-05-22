// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Helpers;

namespace Lunet.Core
{
    public class LunetCommandLine : CommandLineApplication
    {
        public LunetCommandLine() : base(false)
        {
            Name = "lunet";
            FullName = "Lunet Static Website Generator";
            Description = "LunetCommand to generate static website";
            HandleResponseFiles = false;
            AllowArgumentSeparator = true;

            HelpOption("-h|--help");

            VersionOption("-v|--version", LunetVersion.AssemblyVersion, LunetVersion.AssemblyVersionInfo);

            // The defines to setup before initializing config.sban
            Defines = Option("-d|--define <variable=value>", "Defines a site variable", CommandOptionType.MultipleValue);

            OutputDirectory = Option("-o|--output-dir <dir>", "The output directory of the generated website. Default is '.lunet/www'", CommandOptionType.SingleValue);

            InputDirectory = Option("-i|--input-dir <dir>", "The input directory of the website content to generate from. Default is '.'", CommandOptionType.SingleValue);

            // New command
            NewCommand = Command("new", newApp =>
            {
                newApp.Description = "Creates a new site or content";

                CommandArgument typeArgument = newApp.Argument("<type>", "Type of the content to generate");

                // TODO: List the supported type on --help -h
                newApp.HelpOption("-h|--help");
            }, false);

            // config command
            ConfigCommand = Command("config", newApp =>
            {
                newApp.Description = "Displays the configuration variables from an existing config or the defaults";
                newApp.HelpOption("-h|--help");
            }, false);

            // The run command
            RunCommand = Command("run", newApp =>
            {
                newApp.Description = "Generates the website";
                newApp.HelpOption("-h|--help");
            }, false);

            // The server command
            ServerCommand = Command("server", newApp =>
            {
                newApp.Description = "Generates the website, runs a web server and watches for changes";
                var noWatch = newApp.Option("-n|--no-watch", "Disables watching files and triggering of a new run", CommandOptionType.NoValue);
                newApp.HelpOption("-h|--help");
            }, false);
        }

        public CommandLineApplication NewCommand { get; }

        public CommandLineApplication ConfigCommand { get; }
        
        public CommandLineApplication RunCommand { get; }

        public CommandLineApplication ServerCommand { get; }

        public CommandOption Defines { get; }

        public CommandOption OutputDirectory { get; }

        public CommandOption InputDirectory { get; }
    }
}