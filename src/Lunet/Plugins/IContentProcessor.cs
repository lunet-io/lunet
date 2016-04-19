using Lunet.Core;

namespace Lunet.Plugins
{
    public interface IContentProcessor : ISiteProcessor
    {
        ContentResult TryProcess(ContentObject file);
    }
}