// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Resources;
using Lunr;
using Zio;
using Zio.FileSystems;

namespace Lunet.Search;

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
        var docs = new List<Document>();
        var index = Task.Run(async () => await Index.Build(async builder =>
        {
            builder.MetadataAllowList.Add("position");
            builder.AddField("href", 3);
            builder.AddField("title", 2);
            builder.AddField("body", 1);

            builder.Separator = c => !char.IsLetterOrDigit(c);

            foreach (var (file, plainText) in _searchContentList)
            {
                var doc = new Document
                {
                    {"id", file.Url ?? string.Empty},
                    {"href", file.Url ?? string.Empty},
                    {"title", file.Title ?? string.Empty},
                    {"body", plainText ?? string.Empty},
                };
                docs.Add(doc);
                await builder.Add(doc);
            }
        })).Result;


        var dbContent = new DbContent()
        {
            Index = index,
        };
        foreach (var doc in docs)
        {
            var rawDoc = new Dictionary<string, object>(doc);
            rawDoc.Remove("id");
            dbContent.Docs.Add((string)doc["id"], rawDoc);
        }

        var db = JsonSerializer.Serialize(dbContent, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

        var dbPathOnDisk = Path.GetTempFileName();
        File.WriteAllText(dbPathOnDisk, db, new UTF8Encoding(false));

        // Add our dynamic content to the output
        var fs = new PhysicalFileSystem();
        var srcPath = fs.ConvertPathFromInternal(dbPathOnDisk);
        var content = new FileContentObject(Site, new FileSystemItem(fs, srcPath, false), path: OutputUrl.ChangeExtension("json"));
        Site.DynamicPages.Add(content);

        // TODO: make it configurable by selecting which bundle will receive the search/db
        var defaultBundle = Plugin.BundlePlugin.GetOrCreateBundle(null);

        var lunr = Plugin.ResourcePlugin.TryLoadResource("npm", "lunr", "2.3.8", ResourceInstallFlags.Private);
        if (lunr is null)
        {
            Site.Error("Unable to load npm resource `lunr@2.3.8` for lunr search engine.");
            return;
        }

        defaultBundle.AddJs(lunr, "lunr.js", mode: "");
        if (Plugin.Worker)
        {
            defaultBundle.AddJs("/modules/search/lunr/lunet-search-lunr.js", mode: "");
        }
        else
        {
            defaultBundle.AddJs("/modules/search/lunr/lunet-search-lunr.js", mode: "");
            //// Insert content before the others to make sure they are loaded async ASAP
            //defaultBundle.InsertLink(0, BundleObjectProperties.ContentType, "/modules/search/sqlite/lunet-sql-wasm.wasm", "/js/lunet-sql-wasm.wasm");
            //defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-search.js");
            //defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/search/sqlite/lunet-sql-wasm.js");
        }
    }
        
    private static readonly Regex PunctuationRegex = new Regex(@"[^\w]+");
        
    private static string SquashPunctuation(string text)
    {
        return PunctuationRegex.Replace(text, " ").Trim();
    }


    private class DbContent
    {
        public DbContent()
        {
            Docs = new Dictionary<string, Dictionary<string, object>>();
        }

        public Dictionary<string, Dictionary<string, object>> Docs { get; }

        public Index Index { get; set; } = null!;
    }
}
