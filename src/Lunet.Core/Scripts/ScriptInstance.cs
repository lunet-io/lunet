using Lunet.Core;
using Scriban.Syntax;

namespace Lunet.Scripts
{
    public class ScriptInstance
    {
        public ScriptInstance(bool hasErrors, string sourceFilePath, IFrontMatter frontMatter, ScriptPage template)
        {
            HasErrors = hasErrors;
            SourceFilePath = sourceFilePath;
            FrontMatter = frontMatter;
            Template = template;
        }

        public readonly bool HasErrors;

        public readonly string SourceFilePath;

        public readonly IFrontMatter FrontMatter;

        public readonly ScriptPage Template;
    }
}