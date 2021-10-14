using System;
using Lunet.Core;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Yaml
{
    public class YamlFrontMatterParser : IFrontMatterParser
    {
        public bool CanHandle(ReadOnlySpan<byte> header)
        {
            return header[0] == (byte)'-' && header[1] == (byte)'-' && header[2] == (byte)'-';
        }

        public bool CanHandle(ReadOnlySpan<char> header)
        {
            return header[0] == '-' && header[1] == '-' && header[2] == '-';
        }

        public IFrontMatter TryParse(string text, string sourceFilePath, out TextPosition position)
        {
            var frontMatter = YamlUtil.FromYamlFrontMatter(text, out position, sourceFilePath);
            if (frontMatter is ScriptObject obj)
            {
                return new YamlFrontMatter(obj);
            }
            return null;
        }

        private class YamlFrontMatter : IFrontMatter
        {
            public YamlFrontMatter(ScriptObject o)
            {
                Object = o;
            }
            
            public ScriptObject Object { get; }

            public void Evaluate(TemplateContext context)
            {
                var dest = context.CurrentGlobal;
                foreach (var keyPair in Object)
                {
                    dest.SetValue(keyPair.Key, keyPair.Value, false);
                }
            }
        }
    }
}