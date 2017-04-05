
namespace Lunet.Core
{
    public abstract class ContentProcessor<TPlugin> : ProcessorBase<TPlugin>, IContentProcessor where TPlugin : ISitePlugin
    {
        protected ContentProcessor(TPlugin plugin) : base(plugin)
        {
        }

        public abstract ContentResult TryProcess(ContentObject file);
    }
}