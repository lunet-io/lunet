using Lunet.Runtime;

namespace Lunet.Plugins
{
    public abstract class ContentProcessor : ProcessorBase, IContentProcessor
    {
        public abstract ContentResult TryProcess(ContentObject file);
    }
}