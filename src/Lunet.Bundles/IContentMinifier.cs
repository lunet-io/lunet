namespace Lunet.Bundles
{
    public interface IContentMinifier
    {
        string Name { get; }

        string Minify(string type, string content);
    }
}