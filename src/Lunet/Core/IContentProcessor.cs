namespace Lunet.Core
{
    public interface IContentProcessor : ISiteProcessor
    {
        ContentResult TryProcess(ContentObject file);
    }
}