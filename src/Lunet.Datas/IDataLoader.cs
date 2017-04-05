using System.IO;

namespace Lunet.Datas
{
    public interface IDataLoader
    {
        bool CanHandle(string fileExtension);

        object Load(FileInfo file);
    }
}