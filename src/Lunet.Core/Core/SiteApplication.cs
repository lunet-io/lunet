// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Lunet.Core.Commands;
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;

namespace Lunet.Core;

public class SiteApplication
{
    private readonly CommandApp _app;

    public SiteApplication(SiteConfiguration? config = null)
    {
        Config = config ?? new SiteConfiguration();
        DefinesOption = new List<string>();

        var versionText = typeof(SiteObject).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        var infoVersionText = typeof(SiteObject).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        _app = new CommandApp("lunet", "Lunet Static Website Engine", new CommandConfig
        {
            OutputFactory = _ => new TerminalMarkupCommandOutput(),
        });

        _app.Add(new HelpOption("h|help"));
        _app.Add(new VersionOption(infoVersionText ?? versionText ?? "0.0.0", "v|version"));

        _app.Add("d|define=", "Defines a site variable. Example --define my_variable=5.", DefinesOption);
        _app.Add("o|output-dir=", $"The output directory of the generated website. Default is '{SiteFileSystems.DefaultOutputFolderName}'", value => OutputDirectoryOption = value);
        _app.Add("i|input-dir=", "The input directory of the website content to generate from. Default is '.'", value => InputDirectoryOption = value);
        _app.Add("stacktrace", "Shows full stacktrace when an error occurs. Default is false.", _ => ShowStacktraceOnErrorOption = true);
        _app.Add((context, _) =>
        {
            _app.ShowHelp(context.RunConfig);
            return ValueTask.FromResult(0);
        });

        InitCommand = AddCommand("init", "Creates a new website", newApp =>
        {
            var force = false;
            string? folder = null;

            newApp.Add(new HelpOption("h|help"));
            newApp.Add("f|force", "Force the creation of the website even if the folder is not empty", _ => force = true);
            newApp.Add("[folder]", "Destination folder. Default is current directory.", value => folder = value);
            newApp.Add((_, _) =>
            {
                _inputDirectoryFolder = folder ?? ".";
                var initCommand = CreateCommandRunner<InitCommandRunner>();
                initCommand.Force = force;
                return ValueTask.FromResult(0);
            });
        });

        NewCommand = AddCommand("new", "Creates a new content for the website from an archetype", newApp =>
        {
            newApp.Add(new HelpOption("h|help"));
            newApp.Add("<type>", "Type of the content to generate", _ => {});
            newApp.Add((_, _) => ValueTask.FromResult(0));
        });

        ConfigCommand = AddCommand("config", "Displays the configuration variables from an existing config or the defaults", newApp =>
        {
            newApp.Add(new HelpOption("h|help"));
            newApp.Add((_, _) => ValueTask.FromResult(0));
        });

        CleanCommand = AddCommand("clean", "Cleans temporary folder", newApp =>
        {
            newApp.Add(new HelpOption("h|help"));
            newApp.Add((_, _) =>
            {
                CreateCommandRunner<CleanCommandRunner>();
                return ValueTask.FromResult(0);
            });
        });
    }

    public SiteConfiguration Config { get; }

    public Command InitCommand { get; }

    public Command NewCommand { get; }

    public Command ConfigCommand { get; }

    public Command? RunCommand { get; private set; }

    public Command CleanCommand { get; }

    private List<string> DefinesOption { get; }

    private string? OutputDirectoryOption { get; set; }

    private string? InputDirectoryOption { get; set; }

    private string? _inputDirectoryFolder;

    private bool ShowStacktraceOnErrorOption { get; set; }

    public Command AddCommand(string name, string description, Action<Command> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);
        var command = new Command(name, description);
        configure(command);
        _app.Add(command);
        return command;
    }
        
    public SiteApplication Add(SiteModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        module.ConfigureInternal(this);
        return this;
    }

    private void InitializeConfig()
    {
        Config.FileSystems.Initialize(_inputDirectoryFolder ?? InputDirectoryOption, OutputDirectoryOption);
        Config.Defines.Clear();
        Config.Defines.AddRange(DefinesOption);
        Config.ShowStacktraceOnError = ShowStacktraceOnErrorOption;
    }

    public TCommandRunner CreateCommandRunner<TCommandRunner>() where TCommandRunner : ISiteCommandRunner, new()
    {
        InitializeConfig();
        var commandRunner = new TCommandRunner();
        Config.CommandRunners.Add(commandRunner);
        return commandRunner;
    }

    public void Execute(params string[] args)
    {
        DefinesOption.Clear();
        OutputDirectoryOption = null;
        InputDirectoryOption = null;
        _inputDirectoryFolder = null;
        ShowStacktraceOnErrorOption = false;
        _app.RunAsync(args).GetAwaiter().GetResult();
    }
}
