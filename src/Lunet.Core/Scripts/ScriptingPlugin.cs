// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using Scriban;
using Scriban.Syntax;
using Scriban.Parsing;
using Scriban.Runtime;
using Zio;

namespace Lunet.Scripts
{
    public class ScriptingPlugin : SitePlugin
    {
        private readonly ITemplateLoader unauthorizedTemplateLoader;

        private const string IncludesDirectoryName = "includes";

        internal ScriptingPlugin(SiteObject site) : base(site)
        {
            unauthorizedTemplateLoader = new TemplateLoaderUnauthorized(Site);
            GlobalObject = TemplateContext.GetDefaultBuiltinObject();
            SiteFunctions = new ScriptGlobalFunctions(this);
        }

        public ScriptObject GlobalObject { get; }

        /// <summary>
        /// Gets the functions that are only accessible from a sban/script file (and not from a page)
        /// </summary>
        public ScriptGlobalFunctions SiteFunctions { get; }

        /// <summary>
        /// Parses a script with the specified content and path.
        /// </summary>
        /// <param name="scriptContent">Content of the script.</param>
        /// <param name="scriptPath">The script path.</param>
        /// <param name="parsingMode">The parsing mode.</param>
        /// <returns>
        /// The parsed script or null of an error occured
        /// </returns>
        /// <exception cref="System.ArgumentNullException">if <paramref name="scriptContent" /> or <paramref name="scriptPath" /> is null</exception>
        /// <remarks>
        /// If there are any parsing errors, the errors will be logged to the <see cref="Log" /> and <see cref="HasErrors" /> will be set to <c>true</c>.
        /// </remarks>
        public Template ParseScript(string scriptContent, UPath scriptPath, ScriptMode parsingMode)
        {
            if (scriptContent == null) throw new ArgumentNullException(nameof(scriptContent));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));

            // Load parse the template
            var template = Template.Parse(scriptContent, scriptPath.FullName, null, new LexerOptions() { Mode = parsingMode });

            // If we have any errors, log them and set the errors flag on this instance
            if (template.HasErrors)
            {
                LogScriptMessages(template.Messages);
            }

            return template;
        }

        public bool TryImportScript(string scriptText, UPath scriptPath, IDynamicObject scriptObject, ScriptFlags flags, out object result, TemplateContext context = null)
        {
            if (scriptText == null) throw new ArgumentNullException(nameof(scriptText));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));

            result = null;
            context = context ?? new TemplateContext(GlobalObject);

            var scriptConfig = ParseScript(scriptText, scriptPath.FullName, ScriptMode.ScriptOnly);
            if (!scriptConfig.HasErrors)
            {
                if ((flags & ScriptFlags.AllowSiteFunctions) != 0)
                {
                    context.PushGlobal(SiteFunctions);
                }

                context.PushGlobal((ScriptObject)scriptObject);
                context.PushSourceFile(scriptPath.FullName);
                context.EnableOutput = false;
                context.TemplateLoader =  (flags & ScriptFlags.AllowSiteFunctions) != 0 ? unauthorizedTemplateLoader : new TemplateLoaderFromIncludes(Site);

                try
                {
                    result = scriptConfig.Page.Evaluate(context);
                }
                catch (ScriptRuntimeException exception)
                {
                    LogException(exception);
                    return false;
                }
                finally
                {
                    context.PopSourceFile();
                    context.PopGlobal();
                    if ((flags & ScriptFlags.AllowSiteFunctions) != 0)
                    {
                        context.PopGlobal();
                    }
                }
                return true;
            }
            return false;
        }

        public bool TryImportScriptStatement(string scriptStatement, IDynamicObject scriptObject, ScriptFlags flags, out object result, TemplateContext context = null)
        {
            if (scriptStatement == null) throw new ArgumentNullException(nameof(scriptStatement));
            if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));
            return TryImportScript(scriptStatement, "__script__", scriptObject, flags, out result, context);
        }

        public bool TryImportScriptFromFile(FileEntry scriptPath, IDynamicObject scriptObject, ScriptFlags flags, out object result, TemplateContext context = null)
        {
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));
            result = null;

            var scriptExist = scriptPath.Exists;
            if ((flags & ScriptFlags.Expect) != 0)
            {
                if (!scriptExist)
                {
                    Site.Error($"Config file [{scriptPath}] does not exist");
                    return false;
                }
            }

            if (scriptExist)
            {
                var configAsText = scriptPath.ReadAllText();
                return TryImportScript(configAsText, scriptPath.Path, scriptObject, flags, out result, context);
            }
            return true;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public bool TryRunFrontMatter(ScriptPage script, IDynamicObject obj)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (script.FrontMatter == null)
            {
                return false;
            }

            var context = new TemplateContext(GlobalObject);
            context.PushGlobal((ScriptObject)obj);
            try
            {
                context.EnableOutput = false;
                context.TemplateLoader = new TemplateLoaderFromIncludes(Site);

                Site.SetValue(PageVariables.Site, this, true);
                script.FrontMatter.Evaluate(context);
            }
            catch (ScriptRuntimeException exception)
            {
                LogException(exception);
                return false;
            }
            finally
            {
                context.PopGlobal();

                // We don't keep the site variable after this initialization
                Site.Remove(PageVariables.Site);
            }
            return true;
        }


        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public bool TryEvaluate(ContentObject page, ScriptPage script, UPath scriptPath, ScriptObject obj = null, TemplateContext context = null)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));

            context = context ?? new TemplateContext(GlobalObject);
            if (obj != null)
            {
                context.PushGlobal(obj);
            }
            context.PushSourceFile((string)scriptPath);

            var currentScriptObject = (ScriptObject)context.CurrentGlobal;

            try
            {
                context.EnableOutput = true;
                var includeLoader = new TemplateLoaderFromIncludes(Site);
                context.TemplateLoader = includeLoader;

                currentScriptObject.SetValue(PageVariables.Site, Site, true);
                currentScriptObject.SetValue(PageVariables.Page, page, true);

                // TODO: setup include paths for script
                script.Evaluate(context);

                foreach (var includeFile in includeLoader.IncludeFiles)
                {
                    page.Dependencies.Add(new FileContentDependency(includeFile));
                }

            }
            catch (ScriptRuntimeException exception)
            {
                LogException(exception);
                return false;
            }
            finally
            {
                if (obj != null)
                {
                    context.PopGlobal();
                }
                context.PopSourceFile();
                page.Content = context.Output.ToString();

                // We don't keep the site variable after this initialization
                currentScriptObject.Remove(PageVariables.Site);
                currentScriptObject.Remove(PageVariables.Page);
            }
            return true;
        }

        private void LogException(ScriptRuntimeException exception)
        {
            Site.Error(exception.Span, exception.GetReason());

            var parserException = exception as ScriptParserRuntimeException;
            if (parserException != null)
            {
                LogScriptMessages(parserException.ParserMessages);
            }
        }

        private void LogScriptMessages(List<LogMessage> logMessages)
        {
            foreach (var logMessage in logMessages)
            {
                switch (logMessage.Type)
                {
                    case ParserMessageType.Error:
                        Site.Error(logMessage.Span, logMessage.Message);
                        break;

                    case ParserMessageType.Warning:
                        Site.Warning(logMessage.Span, logMessage.Message);
                        break;
                    default:
                        Site.Info(logMessage.Span, logMessage.Message);
                        break;
                }
            }
        }

        private class TemplateLoaderUnauthorized : ITemplateLoader
        {
            private readonly SiteObject site;

            public TemplateLoaderUnauthorized(SiteObject site)
            {
                this.site = site;
            }

            public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
            {
                site.Error(callerSpan, $"The include statement is not allowed from this context. The include [{templateName}] cannot be loaded");
                return null;
            }

            public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
            {
                throw new NotImplementedException();
            }
        }

        private class TemplateLoaderFromIncludes : ITemplateLoader
        {
            private readonly SiteObject site;

            public TemplateLoaderFromIncludes(SiteObject site)
            {
                this.site = site;
                IncludeFiles = new HashSet<FileEntry>();
            }

            public HashSet<FileEntry> IncludeFiles { get; }

            public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
            {
                templateName = templateName.Trim();
                if (templateName.Contains("..") || templateName.StartsWith("/") || templateName.StartsWith("\\"))
                {
                    site.Error(callerSpan, $"The include [{templateName}] cannot contain '..' or start with '/' or '\\'");
                    return null;
                }
                var templatePath = UPath.Root / IncludesDirectoryName / templateName;
                return (string) templatePath;
            }

            public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
            {
                var templateFile = new FileEntry(site.MetaFileSystem, templatePath);
                IncludeFiles.Add(templateFile);
                return templateFile.ReadAllText();
            }
        }
    }
}