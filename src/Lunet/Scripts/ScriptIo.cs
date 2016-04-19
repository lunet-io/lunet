// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Lunet.Resources;

namespace Lunet.Scripts
{
    public class ScriptIo : DynamicObject
    {
        private readonly SiteObject site;

        private delegate void CopyFunctionDelegate(params object[] args);

        public ScriptIo(ScriptManager manager) : base(manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            this.site = manager.Site;

            manager.GlobalObject.SetValue("io", this, true);
            Import("copy", (CopyFunctionDelegate) CopyFunction);
        }

        public void Copy(List<string> fromFiles, string outputPath, ResourceObject resourceContext = null)
        {
            if (fromFiles == null) throw new ArgumentNullException(nameof(fromFiles));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            if (fromFiles.Count == 0)
            {
                return;
            }

            var isOutputDirectory = outputPath.EndsWith("\\") || outputPath.EndsWith("/");
            var rootFolder = resourceContext?.AbsoluteDirectory ?? site.BaseDirectory;

            var normalizeOutput = PathUtil.NormalizeRelativePath(outputPath, isOutputDirectory);
            if (Path.IsPathRooted(normalizeOutput))
            {
                throw new LunetException($"Cannot output to a root directory [{outputPath}].  Only relative is accepted");
            }

            if (!isOutputDirectory && fromFiles.Count > 1)
            {
                throw new LunetException($"Cannot output to a single file [{normalizeOutput}] when input has more than 1 file ({fromFiles.Count})");
            }

            var outputNormalized = Path.Combine(site.OutputDirectory, PathUtil.NormalizeRelativePath(outputPath, isOutputDirectory));

            foreach (var inputFile in fromFiles)
            {
                if (inputFile == null)
                {
                    continue;
                }

                if (isOutputDirectory)
                {
                    
                }

                var inputFileTrim = inputFile.Trim();
                var isInputDirectory = inputFileTrim.EndsWith("\\") || inputFileTrim.EndsWith("/");
                if (isInputDirectory)
                {
                    throw new LunetException($"Input file [{inputFile}] cannot be a directory");
                }

                var inputFileFullPath = PathUtil.NormalizeRelativePath(inputFileTrim, false);
                var inputFullPath  = Path.Combine(rootFolder, inputFileFullPath);

                var relativeOutputFile =
                    PathUtil.NormalizeRelativePath(site.OutputDirectory.GetRelativePath(isOutputDirectory
                        ? Path.Combine(outputNormalized, Path.GetFileName(inputFullPath))
                        : outputNormalized, PathFlags.File), false);

                site.Generator.TryCopyFile(new FileInfo(inputFullPath), relativeOutputFile);
            }
        }

        private void CopyFunction(params object[] args)
        {
            var files = new List<string>();

            ResourceObject resourceContext = null;

            foreach (var arg in args)
            {
                if (arg is string)
                {
                    files.Add((string)arg);
                }
                else if (arg is DynamicObject)
                {
                    var resourceContextArg = ((DynamicObject) arg).Parent as ResourceObject;
                    if (resourceContextArg == null)
                    {
                        throw new LunetException($"Invalid parameter type [{arg}], expecting only string or resource");
                    }

                    if (resourceContext != null)
                    {
                        throw new LunetException($"Invalid resource [{resourceContextArg.Name}] context. Only a single resource context can be used");
                    }
                    resourceContext = resourceContextArg;
                }
                else
                {
                    throw new LunetException($"Invalid parameter type [{arg}], expecting only string or resource");
                }
            }

            if (files.Count < 2)
            {
                throw new LunetException("Expecting at least one or multiple [input] and one [output]");
            }
            var toPath = files[files.Count - 1];
            files.RemoveAt(files.Count - 1);
            Copy(files, toPath, resourceContext);
        }
    }
}