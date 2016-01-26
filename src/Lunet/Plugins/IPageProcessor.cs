using Lunet.Runtime;

namespace Lunet.Plugins
{
    public interface IPageProcessor : ISiteProcessor
    {
        PageProcessResult TryProcess(ContentObject file);
    }
}