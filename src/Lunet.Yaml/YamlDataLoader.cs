using System.IO;
using Lunet.Datas;

namespace Lunet.Yaml
{
    public class YamlDataLoader : IDataLoader
    {
        public bool CanHandle(string fileExtension)
        {
            var fileExt = fileExtension.ToLowerInvariant();
            return fileExt == ".yml" || fileExt == ".yaml";
        }

        public object Load(FileInfo file)
        {
            var text = File.ReadAllText(file.FullName);
            return YamlUtil.FromYaml(text);
        }
    }
}