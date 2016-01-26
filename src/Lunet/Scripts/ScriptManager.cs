// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Lunet.Runtime;
using Textamina.Scriban;
using Textamina.Scriban.Parsing;
using Textamina.Scriban.Runtime;

namespace Lunet.Scripts
{
    public class ScriptManager : ManagerBase
    {
        internal ScriptManager(SiteObject site) : base(site)
        {
            Context = new TemplateContext();
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

        public bool TryImportScript(string scriptText, string scriptPath, ScriptObject scriptObject, bool allowInclude = false)
        {
            if (scriptText == null) throw new ArgumentNullException(nameof(scriptText));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));

            var scriptConfig = ParseScript(scriptText, scriptPath, ParsingMode.ScriptOnly);
            if (!scriptConfig.HasErrors)
            {
                Context.PushGlobal(scriptObject);
                Context.PushSourceFile(scriptPath);
                Context.Options.TemplateLoader = allowInclude ? /*TODO*/ null : new TemplateLoaderUnauthorized(Site);

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

        public bool TryImportScriptFromFile(string scriptPath, ScriptObject scriptObject, bool expectScript = false, bool allowInclude = false)
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
        public bool TryRunFrontMatter(ScriptPage script, ScriptObject obj)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (script.FrontMatter == null)
            {
                return false;
            }

            var context = Context;
            context.PushGlobal(obj);
            try
            {
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
                context.Output.Clear();
                // We don't keep the site variable after this initialization
                Site.Remove(PageVariables.Site);
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
            
            var context = Context;
            if (obj != null)
            {
                context.PushGlobal(obj);
            }
            context.PushSourceFile(scriptPath);
            context.Output.Clear();

            var currentScriptObject = context.CurrentGlobal;

            try
            {
                currentScriptObject.SetValue(PageVariables.Site, Site, true);
                currentScriptObject.SetValue(PageVariables.Page, page, true);

                // TODO: setup include paths for script
                script.Evaluate(context);
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
            Site.Error(exception.Span, exception.Message);

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
    }
}