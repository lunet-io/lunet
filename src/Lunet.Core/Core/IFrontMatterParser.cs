using System;
using Scriban.Parsing;

namespace Lunet.Core
{
    public interface IFrontMatterParser
    {
        bool CanHandle(ReadOnlySpan<char> header);

        IFrontMatter TryParse(string text, string sourceFilePath, out TextPosition position);
    }
}