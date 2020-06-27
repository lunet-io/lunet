namespace Lunet.Core
{
    public interface IContentProcessor : ISiteProcessor
    {
        ContentResult TryProcessContent(ContentObject file, ContentProcessingStage stage);
    }
}