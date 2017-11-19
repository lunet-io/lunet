using System.IO;
using Lunet.Datas;
using Zio;

namespace Lunet.Yaml
{
    public class YamlDataLoader : IDataLoader
    {
        public bool CanHandle(string fileExtension)
        {
            var fileExt = fileExtension.ToLowerInvariant();
            return fileExt == ".yml" || fileExt == ".yaml";
        }

        public object Load(FileEntry file)
        {
            // TODO: Add ReadAllText Methods to FileEntry
            var text = file.FileSystem.ReadAllText(file.Path);
            return YamlUtil.FromYaml(text);
        }
    }
}