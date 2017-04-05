// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Microsoft.Extensions.Logging;

namespace Lunet.Datas
{
    public class DatasProcessor : ProcessorBase<DatasPlugin>
    {
        public const string DataDirectory = "data";

        public DatasProcessor(DatasPlugin plugin) : base(plugin)
        {
        }

        public override void Process()
        {
            // We first preload all data object into the site.data object

            foreach (var directory in Site.MetaFolders)
            {
                var dataFolder = directory.GetSubFolder(DataDirectory);
                if (dataFolder.Exists)
                {
                    foreach (var fileInfo in dataFolder.Info.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        var dataObject = GetDataObject(new FolderInfo(fileInfo.Directory), dataFolder);

                        foreach (var loader in Plugin.DataLoaders)
                        {
                            if (loader.CanHandle(fileInfo.Extension))
                            {
                                try
                                {
                                    object result = loader.Load(fileInfo);
                                    var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);

                                    // If a datafile has been already loaded, we have probably something wrong 
                                    // (Typically a folder or file loaded with the same name without the extension)
                                    if (dataObject.ContainsKey(nameWithoutExtension))
                                    {
                                        Site.Warning($"Cannot load the data file [{Site.GetRelativePath(fileInfo.FullName, PathFlags.File)}] as there is already an entry with the same name [{nameWithoutExtension}]");
                                    }
                                    else
                                    {
                                        dataObject[nameWithoutExtension] = result;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Site.Log.LogError((EventId)0, ex, $"Error while loading data file [{Site.GetRelativePath(fileInfo.FullName, PathFlags.File)}]. Reason: {ex.GetReason()}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private DataObject GetDataObject(FolderInfo folder, FolderInfo data)
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