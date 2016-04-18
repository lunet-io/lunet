// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Helpers;
using Lunet.Runtime;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Scripts
{
    public class ScriptManager : ManagerBase
    {
        private readonly ITemplateLoader unauthorizedTemplateLoader;
        private readonly ITemplateLoader templateLoaderFromIncludes;

        private const string IncludesDirectoryName = "includes";

        internal ScriptManager(SiteObject site) : base(site)
        {
            Context = new TemplateContext();
            unauthorizedTemplateLoader = new TemplateLoaderUnauthorized(Site);
            templateLoaderFromIncludes = new TemplateLoaderFromIncludes(Site);
        }

        public TemplateContext Context { get; }

        /// <summary>
        /// Parses a script with the specified content and path.
        /// </summary>
        /// <param name="scriptContent">Content of the script.</param>
        /// <param name="scriptPath">The script path.</param>
        /// <param name="parsingMode">The parsing mode.</param>
        /// <returns>The parsed script or null of an error occured</returns>
        /// <exception cref="System.ArgumentNullException">if <paramref name="scriptContent"/> or <paramref name="scriptPath"/> is null</exception>
        /// <remarks>
        /// If there are any parsing errors, the errors will be logged to the <see cref="Log"/> and <see cref="HasErrors"/> will be set to <c>true</c>.
        /// </remarks>
        public Template ParseScript(string scriptContent, string scriptPath, ParsingMode parsingMode)
        {
            if (scriptContent == null) throw new ArgumentNullException(nameof(scriptContent));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));

            // Load parse the template
            var template = Template.Parse(scriptContent, scriptPath,
                new TemplateOptions()
                {
                    Parser =
                    {
                        Mode = parsingMode
                    }
                });

            // If we have any errors, log them and set the errors flag on this instance
            if (template.HasErrors)
            {
                LogScriptMessages(template.Messages);
            }

            return template;
        }

        public bool TryImportScript(string scriptText, string scriptPath, IDynamicObject scriptObject, bool allowInclude = false)
        {
            if (scriptText == null) throw new ArgumentNullException(nameof(scriptText));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));

            var scriptConfig = ParseScript(scriptText, scriptPath, ParsingMode.ScriptOnly);
            if (!scriptConfig.HasErrors)
            {
                Context.PushGlobal((ScriptObject)scriptObject);
                Context.PushSourceFile(scriptPath);
                Context.Options.TemplateLoader = allowInclude ? templateLoaderFromIncludes : unauthorizedTemplateLoader;

                try
                {
                    scriptConfig.Page.Evaluate(Context);
                }
                catch (ScriptRuntimeException exception)
                {
                    LogException(exception);
                    return false;
                }
                finally
                {
                    Context.PopSourceFile();
                    Context.PopGlobal();
                    Context.Output.Clear();
                }
                return true;
            }
            return false;
        }

        public bool TryImportScriptFromFile(string scriptPath, IDynamicObject scriptObject, bool expectScript = false, bool allowInclude = false)
        {
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));

            var scriptExist = File.Exists(scriptPath);
            if (expectScript)
            {
                if (!scriptExist)
                {
                    Site.Error($"Config file [{scriptPath}] does not exist");
                    return false;
                }
            }

            if (scriptExist)
            {
                var configAsText = File.ReadAllText(scriptPath);
                return TryImportScript(configAsText, scriptPath, scriptObject, allowInclude);
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

            Context.PushGlobal((ScriptObject)obj);
            try
            {
                Context.Options.Parser.Mode = ParsingMode.FrontMatter;
                Context.Options.TemplateLoader = templateLoaderFromIncludes;

                Site.DynamicObject.SetValue(PageVariables.Site, this.DynamicObject, true);
                script.FrontMatter.Evaluate(Context);
            }
            catch (ScriptRuntimeException exception)
            {
                LogException(exception);
                return false;
            }
            finally
            {
                Context.PopGlobal();

                Context.Output.Clear();
                Context.Options.TemplateLoader = unauthorizedTemplateLoader;

                // We don't keep the site variable after this initialization
                Site.DynamicObject.Remove(PageVariables.Site);
            }
            return true;
        }


        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public bool TryEvaluate(ContentObject page, ScriptPage script, string scriptPath, ScriptObject obj = null)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            
            if (obj != null)
            {
                Context.PushGlobal(obj);
            }
            Context.PushSourceFile(scriptPath);
            Context.Output.Clear();

            var currentScriptObject = Context.CurrentGlobal;

            try
            {
                Context.Options.Parser.Mode = ParsingMode.Default;
                Context.Options.TemplateLoader = templateLoaderFromIncludes;

                currentScriptObject.SetValue(PageVariables.Site, Site.DynamicObject, true);
                currentScriptObject.SetValue(PageVariables.Page, page.DynamicObject, true);

                // TODO: setup include paths for script
                script.Evaluate(Context);
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
                    Context.PopGlobal();
                }
                Context.PopSourceFile();
                page.Content = Context.Output.ToString();

                Context.Options.TemplateLoader = unauthorizedTemplateLoader;

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

            public string Load(TemplateContext context, SourceSpan callerSpan, string templateName, out string templateFilePath)
            {
                templateFilePath = null;
                site.Error(callerSpan, $"The include statement is not allowed from this context. The include [{templateName}] cannot be loaded");
                return null;
            }
        }

        private class TemplateLoaderFromIncludes : ITemplateLoader
        {
            private readonly SiteObject site;

            public TemplateLoaderFromIncludes(SiteObject site)
            {
                this.site = site;
            }

            public string Load(TemplateContext context, SourceSpan callerSpan, string templateName, out string templateFilePath)
            {
                templateFilePath = null;
                templateName = templateName.Trim();
                if (templateName.Contains("..") || templateName.StartsWith("/") || templateName.StartsWith("\\"))
                {
                    site.Error(callerSpan, $"The include [{templateName}] cannot contain '..' or start with '/' or '\\'");
                    return null;
                }

                foreach (var directory in site.Meta.Directories)
                {
                    var includePath = Path.Combine(directory.FullName, IncludesDirectoryName, templateName);
                    if (File.Exists(includePath))
                    {
                        templateFilePath = includePath;
                        return File.ReadAllText(includePath);
                    }
                }

                return null;
            }
        }
    }
}