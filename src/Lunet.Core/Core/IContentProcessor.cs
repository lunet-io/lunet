using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    public interface IContentProcessor : ISiteProcessor
    {
        ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage);
    }

    public delegate void TryProcessPreContentDelegate(UPath path, ref ScriptObject preContent);
}