// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lunet.Core.Commands;
using Lunet.Helpers;
using Spectre.Console;
using Zio;
using Zio.FileSystems;

namespace Lunet.Core;

public class SiteApplication : CommandLineApplication, IEnumerable
{
    private readonly List<SiteModule> _modules;
        
    public SiteApplication(SiteConfiguration config = null)
    {
        Config = config ?? new SiteConfiguration();
        _modules = new List<SiteModule>();

        Name = "lunet";
        FullName = "Lunet Static Website Engine";
        Description = "LunetCommand to generate static website";
        HandleResponseFiles = false;
        AllowArgumentSeparator = true;

        HelpOption("-h|--help");

        var versionText = typeof(SiteObject).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        var infoVersionText = typeof(SiteObject).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        var version = VersionOption("-v|--version", versionText, infoVersionText);

        // The defines to setup before initializing config.sban
        DefinesOption = Option("-d|--define <variable=value>", "Defines a site variable", CommandOptionType.MultipleValue);
        OutputDirectoryOption = Option("-o|--output-dir <dir>", $"The output directory of the generated website. Default is '{SiteFileSystems.DefaultOutputFolderName}'", CommandOptionType.SingleValue);
        InputDirectoryOption = Option("-i|--input-dir <dir>", "The input directory of the website content to generate from. Default is '.'", CommandOptionType.SingleValue);

        ShowStacktraceOnErrorOption = Option("--stacktrace", "Shows full stacktrace when an error occurs. Default is false.", CommandOptionType.NoValue);

        Invoke = () =>
        {
            if (!this.OptionHelp.HasValue() || !version.HasValue())
            {
                this.ShowHint();
            }

            if (RemainingArguments.Count > 0)
            {
                AnsiConsole.WriteLine($"[red]Invalid command arguments : {Markup.Escape(string.Join(" ", RemainingArguments))}[/]");
            }
        };

        // New command
        InitCommand = Command("init", newApp =>
        {
            newApp.Description = "Creates a new website";

            var forceOption = newApp.Option("-f|--force",
                "Force the creation of the website even if the folder is not empty", CommandOptionType.NoValue);

            CommandArgument folderArgument = newApp.Argument("[folder]", "Destination folder. Default is current directory.");

            // TODO: List the supported type on --help -h
            newApp.HelpOption("-h|--help");

            newApp.Invoke = () =>
            {
                var inputFolder = folderArgument.Value ?? ".";
                if (InputDirectoryOption.Values.Count > 0)
                {
                    InputDirectoryOption.Values[0] = inputFolder;
                }
                else
                {
                    InputDirectoryOption.Values.Add(inputFolder);
                }

                var initCommand = CreateCommandRunner<InitCommandRunner>();
                initCommand.Force = forceOption.HasValue();
            };
        });


        // New command
        NewCommand = Command("new", newApp =>
        {
            newApp.Description = "Creates a new content for the website from an archetype";

            CommandArgument typeArgument = newApp.Argument("<type>", "Type of the content to generate");

            // TODO: List the supported type on --help -h
            newApp.HelpOption("-h|--help");
        });

        // config command
        ConfigCommand = Command("config", newApp =>
        {
            newApp.Description = "Displays the configuration variables from an existing config or the defaults";
            newApp.HelpOption("-h|--help");
        });

        // The clean command
        CleanCommand = Command("clean", newApp =>
        {
            newApp.Description = "Cleans temporary folder";
            newApp.HelpOption("-h|--help");

            newApp.Invoke = () =>
            {
                CreateCommandRunner<CleanCommandRunner>();
            };

        });
    }

    public SiteConfiguration Config { get; }

    public CommandLineApplication InitCommand { get; }

    public CommandLineApplication NewCommand { get; }

    public CommandLineApplication ConfigCommand { get; }

    public CommandLineApplication RunCommand { get; }

    public CommandLineApplication CleanCommand { get; }

    private CommandOption DefinesOption { get; }

    private CommandOption OutputDirectoryOption { get; }

    private CommandOption InputDirectoryOption { get; }
        
    private CommandOption ShowStacktraceOnErrorOption { get; }
        
    public SiteApplication Add(SiteModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        module.ConfigureInternal(this);
        return this;
    }

    private void InitializeConfig()
    {
        Config.FileSystems.Initialize(InputDirectoryOption.Value(), OutputDirectoryOption.Value());
        Config.Defines.Clear();
        Config.Defines.AddRange(DefinesOption.Values);
        Config.ShowStacktraceOnError = ShowStacktraceOnErrorOption.HasValue();
    }

    public TCommandRunner CreateCommandRunner<TCommandRunner>() where TCommandRunner : ISiteCommandRunner, new()
    {
        InitializeConfig();
        var commandRunner = new TCommandRunner();
        Config.CommandRunners.Add(commandRunner);
        return commandRunner;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) _modules).GetEnumerator();
    }
}