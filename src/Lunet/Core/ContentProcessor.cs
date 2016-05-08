namespace Lunet.Core
{
    public abstract class ContentProcessor : ProcessorBase, IContentProcessor
    {
        public abstract ContentResult TryProcess(ContentObject file);
    }
}