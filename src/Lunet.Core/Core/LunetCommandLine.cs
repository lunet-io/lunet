// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Lunet.Helpers;
using Zio;
using Zio.FileSystems;

namespace Lunet.Core
{
    public class LunetCommandLine : CommandLineApplication
    {
        private readonly SiteObject site;

        public LunetCommandLine(SiteObject site) : base(false)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            this.site = site;
            Name = "lunet";
            FullName = "Lunet Static Website Engine";
            Description = "LunetCommand to generate static website";
            HandleResponseFiles = false;
            AllowArgumentSeparator = true;

            HelpOption("-h|--help");

            var list = typeof(SiteFactory).GetTypeInfo().Assembly.CustomAttributes.ToList();


            var versionText = typeof(SiteFactory).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var infoVersionText = typeof(SiteFactory).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            var version = VersionOption("-v|--version", versionText, infoVersionText);

            // The defines to setup before initializing config.sban
            Defines = Option("-d|--define <variable=value>", "Defines a site variable", CommandOptionType.MultipleValue);
            OutputDirectory = Option("-o|--output-dir <dir>", $"The output directory of the generated website. Default is '{SiteObject.DefaultOutputFolderName}'", CommandOptionType.SingleValue);
            InputDirectory = Option("-i|--input-dir <dir>", "The input directory of the website content to generate from. Default is '.'", CommandOptionType.SingleValue);

            this.Invoke = () =>
            {
                if (!this.OptionHelp.HasValue() || !version.HasValue())
                {
                    this.ShowHint();
                }

                if (RemainingArguments.Count > 0)
                {
                    Reporter.Output.WriteLine($"Invalid command arguments : {string.Join(" ",RemainingArguments)}".Red());
                    return 1;
                }
                
                return 0;
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
                    HandleCommonOptions();

                    throw new NotImplementedException("TODO: Implement BaseFolder");
                    //site.BaseFolder = folderArgument.Value ?? ".";
                    try
                    {
                        site.Create(forceOption.HasValue());
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        site.Error($"Unexpected exception while trying to copy files: {ex.GetReason()}");
                        return 1;
                    }
                };
            }, false);


            // New command
            NewCommand = Command("new", newApp =>
            {
                newApp.Description = "Creates a new content for the website from an archetype";

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
            RunCommand = Command("build", newApp =>
            {
                newApp.Description = "Builds the website";
                newApp.HelpOption("-h|--help");

                newApp.Invoke = () =>
                {
                    HandleCommonOptions();
                    site.Build();
                    return site.HasErrors ? 1 : 0;
                };

            }, false);

            // The clean command
            CleanCommand = Command("clean", newApp =>
            {
                newApp.Description = "Cleans temporary folder";
                newApp.HelpOption("-h|--help");

                newApp.Invoke = () =>
                {
                    HandleCommonOptions();
                    return site.Clean();
                };

            }, false);
        }

        public CommandLineApplication InitCommand { get; }

        public CommandLineApplication NewCommand { get; }

        public CommandLineApplication ConfigCommand { get; }
        
        public CommandLineApplication RunCommand { get; }

        public CommandLineApplication CleanCommand { get; }

        public CommandOption Defines { get; }

        public CommandOption OutputDirectory { get; }

        public CommandOption InputDirectory { get; }

        public void HandleCommonOptions()
        {
            var baseFolder = Path.GetFullPath(InputDirectory.HasValue() ? InputDirectory.Value() : Environment.CurrentDirectory);

            var diskfs = new PhysicalFileSystem();

            var siteFileSystem = new SubFileSystem(diskfs, diskfs.ConvertPathFromInternal(baseFolder));
            site.SiteFileSystem = siteFileSystem;

            // Add defines
            foreach (var value in Defines.Values)
            {
                site.AddDefine(value);
            }

            site.TempFileSystem = new SubFileSystem(siteFileSystem, UPath.Root / SiteObject.TempFolderName);

            var outputFolder = OutputDirectory.HasValue()
                ? OutputDirectory.Value()
                : Path.Combine(baseFolder, SiteObject.TempFolderName + "/" +  SiteObject.DefaultOutputFolderName);

            site.OutputFileSystem = new SubFileSystem(diskfs, diskfs.ConvertPathToInternal(outputFolder));
        }
    }
}