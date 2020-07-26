using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Resources;
using Lunr;
using Zio;
using Zio.FileSystems;

namespace Lunet.Search
{
    /// <summary>
    /// Search based on sqlite.
    /// </summary>
    public class LunrSearchEngine : SearchEngine
    {
        private readonly List<(ContentObject, string)> _searchContentList;

        public const string EngineName = "lunr";

        public LunrSearchEngine(SearchPlugin plugin) : base(plugin, EngineName)
        {
            _searchContentList = new List<(ContentObject, string)>();
        }

        public override void Initialize()
        {
        }

        public override void ProcessSearchContent(ContentObject file, string plainText)
        {
            _searchContentList.Add((file, plainText));
        }

        public override void Terminate()
        {
            var index = Task.Run(() => Index.Build(async builder =>
            {
                builder.AddField("id", 3);
                builder.AddField("title", 2);
                builder.AddField("body", 1);

                foreach (var (file, plainText) in _searchContentList)
                {
                    await builder.Add(new Document
                    {
                        {"id", file.Url ?? string.Empty},
                        {"title", file.Title ?? string.Empty},
                        {"body", plainText ?? string.Empty},
                    });
                }
            })).Result;
            
            var db = index.ToJson();

            var dbPathOnDisk = Path.GetTempFileName();
            File.WriteAllText(dbPathOnDisk, db, new UTF8Encoding(false));

            // Add our dynamic content to the output
            var fs = new PhysicalFileSystem();
            var srcPath = fs.ConvertPathFromInternal(dbPathOnDisk);
            var content = new ContentObject(Site, new FileEntry(fs, srcPath), path: OutputUrl);
            Site.DynamicPages.Add(content);

            // TODO: make it configurable by selecting which bundle will receive the search/db
            var defaultBundle = Plugin.BundlePlugin.GetOrCreateBundle(null);

            var lunr = Plugin.ResourcePlugin.TryLoadResource("npm", "lunr", "2.3.8", ResourceInstallFlags.Private);

            defaultBundle.AddJs(lunr, "lunr.js", mode: "");
            if (Plugin.Worker)
            {
                defaultBundle.AddJs("/modules/search/lunr/lunet-search.js", mode: "");
            }
            else
            {
                defaultBundle.AddJs("/modules/search/lunr/lunet-search.js", mode: "");
                //// Insert content before the others to make sure they are loaded async ASAP
                //defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/sqlite/lunet-sql-wasm.wasm", "/js/lunet-sql-wasm.wasm");
                //defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-search.js");
                //defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-sql-wasm.js");
            }
        }
    }
}