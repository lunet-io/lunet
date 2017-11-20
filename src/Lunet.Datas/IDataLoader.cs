using System.IO;
using Zio;

namespace Lunet.Datas
{
    public interface IDataLoader
    {
        bool CanHandle(string fileExtension);

        object Load(FileEntry file);
    }
}