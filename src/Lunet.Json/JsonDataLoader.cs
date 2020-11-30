using Lunet.Datas;
using Zio;

namespace Lunet.Json
{
    public class JsonDataLoader : IDataLoader
    {
        public bool CanHandle(string fileExtension)
        {
            var fileExt = fileExtension.ToLowerInvariant();
            return fileExt == ".json";
        }

        public object Load(FileEntry file)
        {
            var text = file.ReadAllText();
            return JsonUtil.FromText(text);
        }
    }
}