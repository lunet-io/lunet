using Lunet.Runtime;

namespace Lunet.Plugins
{
    public abstract class PageProcessorBase : ProcessorBase, IPageProcessor
    {
        public abstract PageProcessResult TryProcess(ContentObject file);
    }

}