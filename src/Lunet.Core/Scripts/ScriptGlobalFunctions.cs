// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Scriban.Runtime;

namespace Lunet.Scripts
{
    public class ScriptGlobalFunctions : DynamicObject<ScriptingPlugin>
    {
        private delegate void CopyFunctionDelegate(params object[] args);

        private delegate void LogDelegate(string message);

        public ScriptGlobalFunctions(ScriptingPlugin service) : base(service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            this.Site = service.Site;

            // Add log object
            LogObject = new DynamicObject<ScriptGlobalFunctions>(this);
            service.Builtins.SetValue("log", LogObject, true);
            LogObject.Import("info", (LogDelegate)(message => Site.Info(message)));
            LogObject.Import("error", (LogDelegate)(message => Site.Error(message)));
            LogObject.Import("warn", (LogDelegate)(message => Site.Warning(message)));
            LogObject.Import("debug", (LogDelegate)(message => Site.Debug(message)));
            LogObject.Import("trace", (LogDelegate)(message => Site.Trace(message)));
            LogObject.Import("fatal", (LogDelegate)(message => Site.Fatal(message)));

            //// Import io object
            //var ioObject = new DynamicObject<ScriptGlobalFunctions>(this);
            //service.GlobalObject.SetValue("io", ioObject, true);
            //ioObject.Import("copy", (CopyFunctionDelegate) CopyFunction);

            // Import global function
            service.Builtins.Import("absurl", new Func<string, string>(AbsoluteUrl));

            // Setup global Lunet object
            LunetObject = new LunetObject(Site);
            service.Builtins.SetValue("lunet", LunetObject, true);
        }

        public SiteObject Site { get; }

        public DynamicObject LogObject { get; }

        public LunetObject LunetObject { get; }


        public string AbsoluteUrl(string url)
        {
            url = url ?? string.Empty;
            var builder = StringBuilderCache.Local();

            // site.basepath = ""  or "/my_subproject"
            // site.baseurl = http://myproject.github.io

            // page.url = "/mypage/under/this/folder/"

            // (page.url | absurl) => "http://myproject.github.io/my_sub_project/mypage/under/this/folder/"

            var baseUrl = Site.BaseUrl ?? string.Empty;
            var basePath = Site.BasePath ?? string.Empty;

            builder.Append(baseUrl);

            var baseUrlHasTrailingSlash = baseUrl.EndsWith("/");
            if (baseUrlHasTrailingSlash)
            {
                if (basePath.StartsWith("/"))
                {
                    basePath = basePath.Substring(1);
                }
            }
            else if (!basePath.StartsWith("/") && basePath != string.Empty)
            {
                builder.Append("/");
            }
            builder.Append(basePath);

            var urlStartsWithSlash = url.StartsWith("/");
            if (!basePath.EndsWith("/"))
            {
                if (!urlStartsWithSlash)
                {
                    builder.Append('/');
                }
            }
            else if (urlStartsWithSlash)
            {
                url = url.Substring(1);
            }
            builder.Append(url);


            return builder.ToString();
        }

        /*
        public void Copy(List<string> fromFiles, string outputPath, FolderInfo? rootFolder = null)
        {
            if (fromFiles == null) throw new ArgumentNullException(nameof(fromFiles));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            if (fromFiles.Count == 0)
            {
                return;
            }

            var isOutputDirectory = outputPath.EndsWith("\\") || outputPath.EndsWith("/");
            if (!rootFolder.HasValue)
            {
                rootFolder = Site.BaseFolder;
            }

            var normalizedOutput = PathUtil.NormalizeRelativePath(outputPath, isOutputDirectory);
            if (Path.IsPathRooted(normalizedOutput))
            {
                throw new LunetException($"Cannot output to a root directory [{outputPath}].  Only relative is accepted");
            }

            if (!isOutputDirectory && fromFiles.Count > 1)
            {
                throw new LunetException($"Cannot output to a single file [{normalizedOutput}] when input has more than 1 file ({fromFiles.Count})");
            }

            var normalizedOutputFull = Path.Combine(Site.OutputFolder, normalizedOutput);

            foreach (var inputFile in fromFiles)
            {
                if (inputFile == null)
                {
                    continue;
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
                    PathUtil.NormalizeRelativePath(Site.OutputFolder.GetRelativePath(isOutputDirectory
                        ? Path.Combine(normalizedOutputFull, Path.GetFileName(inputFullPath))
                        : normalizedOutputFull, PathFlags.File), false);

                Site.Content.TryCopyFile(new FileInfo(inputFullPath), relativeOutputFile);
            }
        }
        */

        //private void CopyFunction(params object[] args)
        //{
        //    var files = new List<string>();

        //    ResourceObject resourceContext = null;

        //    foreach (var arg in args)
        //    {
        //        if (arg is string)
        //        {
        //            files.Add((string)arg);
        //        }
        //        else if (arg is ResourceObject)
        //        {
        //            var resourceContextArg = (ResourceObject)arg;
        //            if (resourceContext != null)
        //            {
        //                throw new LunetException($"Invalid resource [{resourceContextArg.Name}] context. Only a single resource context can be used");
        //            }
        //            resourceContext = resourceContextArg;
        //        }
        //        else
        //        {
        //            throw new LunetException($"Invalid parameter type [{arg}], expecting only string or resource");
        //        }
        //    }

        //    if (files.Count < 2)
        //    {
        //        throw new LunetException("Expecting at least one or multiple [input] and one [output]");
        //    }
        //    var toPath = files[files.Count - 1];
        //    files.RemoveAt(files.Count - 1);
        //    Copy(files, toPath, resourceContext);
        //}
    }
}