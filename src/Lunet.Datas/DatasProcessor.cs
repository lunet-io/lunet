// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Zio;

namespace Lunet.Datas
{
    public class DatasProcessor : ProcessorBase<DatasPlugin>
    {
        public const string DataFolderName = "data";

        public static readonly UPath DataFolder = UPath.Root / DataFolderName;

        public DatasProcessor(DatasPlugin plugin) : base(plugin)
        {
        }

        public override void Process(ProcessingStage stage)
        {
            Debug.Assert(stage == ProcessingStage.BeforeInitializing);

            // We first pre-load all data object into the site.data object
            var dataFolder = new DirectoryEntry(Site.MetaFileSystem, DataFolder);
            if (dataFolder.Exists)
            {
                foreach (var fileInfo in dataFolder.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var dataObject = GetDataObject(fileInfo.Directory, dataFolder);

                    foreach (var loader in Plugin.DataLoaders)
                    {
                        if (loader.CanHandle(fileInfo.ExtensionWithDot))
                        {
                            try
                            {
                                object result = loader.Load(fileInfo);
                                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);

                                // If a datafile has been already loaded, we have probably something wrong 
                                // (Typically a folder or file loaded with the same name without the extension)
                                if (dataObject.ContainsKey(nameWithoutExtension))
                                {
                                    Site.Warning($"Cannot load the data file [{fileInfo}] as there is already an entry with the same name [{nameWithoutExtension}]");
                                }
                                else
                                {
                                    dataObject[nameWithoutExtension] = result;
                                }
                            }
                            catch (Exception ex)
                            {
                                Site.Error(ex, $"Error while loading data file [{fileInfo}]. Reason: {ex.GetReason()}");
                            }
                        }
                    }
                }
            }
        }

        private DataObject GetDataObject(DirectoryEntry folder, DirectoryEntry data)
        {
            if (data == folder)
            {
                return Plugin.RootDataObject;
            }

            var parentObject = GetDataObject(folder.Parent, data);

            var scriptObject = parentObject[folder.Name] as DataFolderObject;
            if (scriptObject != null)
            {
                return scriptObject;
            }

            scriptObject = new DataFolderObject(Plugin, folder);
            parentObject[folder.Name] = scriptObject;
            return scriptObject;
        }
    }
}