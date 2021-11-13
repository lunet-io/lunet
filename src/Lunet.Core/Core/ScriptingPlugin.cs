// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Helpers;
using Scriban;
using Scriban.Syntax;
using Scriban.Parsing;
using Scriban.Runtime;
using Zio;

namespace Lunet.Scripts;

public class ScriptingPlugin : SitePlugin
{
    private readonly ITemplateLoader unauthorizedTemplateLoader;
    private readonly TemplateLoaderFromIncludes _templateLoaderFromIncludes;

    private const string IncludesDirectoryName = "includes";

    private readonly ThreadLocal<Stack<LunetTemplateContext>> _lunetTemplateContextFactory;

    internal ScriptingPlugin(SiteObject site) : base(site)
    {
        unauthorizedTemplateLoader = new TemplateLoaderUnauthorized(Site);
        ScribanBuiltins = TemplateContext.GetDefaultBuiltinObject();
        // Add default scriban frontmatter parser
        FrontMatterParsers = new OrderedList<IFrontMatterParser> {new ScribanFrontMatterParser(this)};
        _templateLoaderFromIncludes = new TemplateLoaderFromIncludes(Site);
        _lunetTemplateContextFactory = new ThreadLocal<Stack<LunetTemplateContext>>(() => new Stack<LunetTemplateContext>());
    }

    public ScriptObject ScribanBuiltins { get; }
        
    public OrderedList<IFrontMatterParser> FrontMatterParsers { get; }

    public ScriptFunction CompileAnonymous(string expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        var statements = Template.Parse($"do;{expression};end", lexerOptions: new LexerOptions() { Mode = ScriptMode.ScriptOnly }).Page.Body.Statements;
        if (statements.Count == 1 && statements[0] is ScriptExpressionStatement exprStatement && exprStatement.Expression is ScriptAnonymousFunction anonymous)
        {
            return anonymous.Function;
        }
        throw new ArgumentException("Unable to extract anonymous function from results", nameof(expression));
    }


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
    public ScriptInstance ParseScript(string scriptContent, UPath scriptPath, ScriptMode parsingMode)
    {
        if (scriptContent == null) throw new ArgumentNullException(nameof(scriptContent));
        if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));

        IFrontMatter frontmatter = null;
        TextPosition startPosition = default;
        if (parsingMode == ScriptMode.FrontMatterAndContent && scriptContent.Length > 3)
        {
            var span = scriptContent.AsSpan();
            foreach (var parser in FrontMatterParsers)
            {
                if (parser.CanHandle(span))
                {
                    frontmatter = parser.TryParse(scriptContent, (string) scriptPath, out startPosition);
                    break;
                }
            }
            parsingMode = ScriptMode.Default;
        }

        // Load parse the template
        var template = Template.Parse(scriptContent, scriptPath.FullName, null, new LexerOptions() { StartPosition = startPosition, Mode = parsingMode });

        // If we have any errors, log them and set the errors flag on this instance
        if (template.HasErrors)
        {
            LogScriptMessages(template.Messages);
        }

        return new ScriptInstance(template.HasErrors, (string)scriptPath, frontmatter, template.Page);
    }

    public bool TryImportScript(string scriptText, UPath scriptPath, ScriptObject scriptObject, ScriptFlags flags, out object result, ScriptMode scriptMode = ScriptMode.ScriptOnly)
    {
        if (scriptText == null) throw new ArgumentNullException(nameof(scriptText));
        if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
        if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));

        result = null;

        var scriptResult = ParseScript(scriptText, scriptPath.FullName, scriptMode);
        if (!scriptResult.HasErrors)
        {
            var context = GetOrCreateTemplateContext();
            if ((flags & ScriptFlags.AllowSiteFunctions) != 0)
            {
                context.PushGlobal(Site.Builtins);
            }

            context.PushGlobal((ScriptObject)scriptObject);
            context.PushSourceFile(scriptPath.FullName);
            context.EnableOutput = false;
            context.TemplateLoader =  (flags & ScriptFlags.AllowSiteFunctions) != 0 ? unauthorizedTemplateLoader : _templateLoaderFromIncludes;

            try
            {
                result = scriptResult.Template.Evaluate(context);
            }
            catch (ScriptRuntimeException exception)
            {
                LogException(exception);
                return false;
            }
            finally
            {
                ReleaseTemplateContext(context);
            }

            return true;
        }
        return false;
    }

    private LunetTemplateContext GetOrCreateTemplateContext()
    {
        var list = _lunetTemplateContextFactory.Value;
        return list.Count == 0 ? new LunetTemplateContext(ScribanBuiltins) : list.Pop();
    }

    private void ReleaseTemplateContext(LunetTemplateContext context)
    {
        context.Reset();
        _lunetTemplateContextFactory.Value.Push(context);
    }

    public bool TryImportScriptStatement(string scriptStatement, ScriptObject scriptObject, ScriptFlags flags, out object result)
    {
        if (scriptStatement == null) throw new ArgumentNullException(nameof(scriptStatement));
        if (scriptObject == null) throw new ArgumentNullException(nameof(scriptObject));
        return TryImportScript(scriptStatement, "__script__", scriptObject, flags, out result);
    }

    public bool TryImportInclude(UPath includePath, ScriptObject toObject)
    {
        if (toObject == null) throw new ArgumentNullException(nameof(toObject));
        if (includePath.IsNull) throw new ArgumentNullException(nameof(includePath));
        if (includePath.IsAbsolute) throw new ArgumentException("Include path must be relative", nameof(includePath));
            
        return TryImportScriptFromFile(new FileEntry(Site.MetaFileSystem, UPath.Root / IncludesDirectoryName / includePath), toObject, ScriptFlags.Expect, out _, scriptMode: ScriptMode.Default);
    }

    public bool TryImportScriptFromFile(FileEntry scriptPath, ScriptObject scriptObject, ScriptFlags flags, out object result, ScriptMode scriptMode = ScriptMode.ScriptOnly)
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
            return TryImportScript(configAsText, scriptPath.Path, scriptObject, flags, out result, scriptMode);
        }
        return true;
    }

    /// <summary>
    /// Initializes this instance.
    /// </summary>
    public bool TryRunFrontMatter(IFrontMatter frontMatter, TemplateObject obj, ScriptObject newGlobal = null)
    {
        if (frontMatter == null) throw new ArgumentNullException(nameof(frontMatter));
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var context = GetOrCreateTemplateContext();
        context.PushGlobal(Site.Builtins);
        context.PushGlobal(obj);
        if (newGlobal != null)
        {
            context.PushGlobal(newGlobal);
        }

        var currentGlobal = context.CurrentGlobal;
        try
        {
            context.EnableOutput = false;
            context.TemplateLoader = _templateLoaderFromIncludes;

            currentGlobal.SetValue(PageVariables.Site, Site, true);
            frontMatter.Evaluate(context);
        }
        catch (ScriptRuntimeException exception)
        {
            LogException(exception);
            return false;
        }
        finally
        {
            currentGlobal.Remove(PageVariables.Site);
            ReleaseTemplateContext(context);
        }
        return true;
    }
        
    /// <summary>
    /// Initializes this instance.
    /// </summary>
    public bool TryEvaluatePage(ContentObject page, ScriptPage script, UPath scriptPath, params ScriptObject[] contextObjects)
    {
        if (page == null) throw new ArgumentNullException(nameof(page));
        if (script == null) throw new ArgumentNullException(nameof(script));
        if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));

        var context = GetOrCreateTemplateContext();
        context.PushGlobal(Site.Builtins);
        context.Page = page;
        foreach (var contextObject in contextObjects)
        {
            if (contextObject != null)
            {
                context.PushGlobal(contextObject);
            }
        }
        context.PushSourceFile((string)scriptPath);

        var currentScriptObject = (ScriptObject)context.CurrentGlobal;

        try
        {
            context.EnableOutput = true;
            context.TemplateLoader = _templateLoaderFromIncludes;

            currentScriptObject.SetValue(PageVariables.Site, Site, true);
            currentScriptObject.SetValue(PageVariables.Page, page, true);

            // TODO: setup include paths for script
            script.Evaluate(context);

            //foreach (var includeFile in _templateLoaderFromIncludes.IncludeFiles)
            //{
            //    page.Dependencies.Add(new FileContentDependency(includeFile));
            //}
        }
        catch (ScriptRuntimeException exception)
        {
            LogException(exception);
            return false;
        }
        finally
        {
            // We don't keep the site variable after this initialization
            currentScriptObject.Remove(PageVariables.Site);
            currentScriptObject.Remove(PageVariables.Page);
            page.Content = context.Output.ToString();
            ReleaseTemplateContext(context);
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

    private void LogScriptMessages(LogMessageBag logMessages)
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
        private readonly SiteObject _site;

        public TemplateLoaderUnauthorized(SiteObject site)
        {
            this._site = site;
        }

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            _site.Error(callerSpan, $"The include statement is not allowed from this context. The include [{templateName}] cannot be loaded");
            return null;
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            throw new NotImplementedException();
        }

        public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return new ValueTask<string>(Load(context, callerSpan, templatePath));
        }
    }

    private class TemplateLoaderFromIncludes : ITemplateLoader
    {
        private readonly SiteObject _site;

        private readonly ConcurrentDictionary<string, string> _templates;

        public TemplateLoaderFromIncludes(SiteObject site)
        {
            this._site = site;
            //IncludeFiles = new HashSet<FileEntry>();
            _templates = new ConcurrentDictionary<string, string>();
        }

        // Try to see how/if we can to have IncludeFiles still generated
        // Would have to be a TLS per thread
        //public HashSet<FileEntry> IncludeFiles { get; }

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            templateName = templateName.Trim();
            if (templateName.Contains("..") || templateName.StartsWith("/") || templateName.StartsWith("\\"))
            {
                _site.Error(callerSpan, $"The include [{templateName}] cannot contain '..' or start with '/' or '\\'");
                return null;
            }
            var templatePath = UPath.Root / IncludesDirectoryName / templateName;
            return (string) templatePath;
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            if (!_templates.TryGetValue(templatePath, out var result))
            {
                var templateFile = new FileEntry(_site.MetaFileSystem, templatePath);
                //IncludeFiles.Add(templateFile);
                result = templateFile.ReadAllText();
                _templates.TryAdd(templatePath, result);
            }
            return result;
        }

        public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return new ValueTask<string>(Load(context, callerSpan, templatePath));
        }
    }
        
    private class ScribanFrontMatter : IFrontMatter
    {
        public ScribanFrontMatter(ScriptFrontMatter frontmatter)
        {
            Value = frontmatter;
        }

        public ScriptFrontMatter Value { get; }

        public void Evaluate(TemplateContext context)
        {
            context.Evaluate(Value);
        }
    }
        
    private class ScribanFrontMatterParser : IFrontMatterParser
    {
        private readonly ScriptingPlugin _scripting;

        public ScribanFrontMatterParser(ScriptingPlugin scripting)
        {
            _scripting = scripting;
        }
            
        public bool CanHandle(ReadOnlySpan<byte> header)
        {
            return header[0] == (byte)'+' && header[1] == (byte)'+' && header[2] == (byte)'+';
        }

        public bool CanHandle(ReadOnlySpan<char> header)
        {
            return header[0] == '+' && header[1] == '+' && header[2] == '+';
        }

        public IFrontMatter TryParse(string text, string sourceFilePath, out TextPosition position)
        {
            position = default;
            var frontMatter = Template.Parse(text, sourceFilePath, lexerOptions: new LexerOptions() {FrontMatterMarker = "+++", Mode = ScriptMode.FrontMatterOnly});
            if (frontMatter.HasErrors || frontMatter.Page.FrontMatter == null)
            {
                _scripting.LogScriptMessages(frontMatter.Messages);
                return null;
            }
            position = frontMatter.Page.FrontMatter.TextPositionAfterEndMarker;
            return new ScribanFrontMatter(frontMatter.Page.FrontMatter);
        }
    }
}


public class LunetTemplateContext : TemplateContext
{
    public LunetTemplateContext(ScriptObject builtin) : base(builtin)
    {
        Defaults();
    }
        
    public ContentObject Page { get; set; }

    private void Defaults()
    {
        LoopLimit = int.MaxValue;
        RecursiveLimit = int.MaxValue;
        LimitToString = 0;
    }

    public override void Reset()
    {
        base.Reset();
        Page = null;
    }
}