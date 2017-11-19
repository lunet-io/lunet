using System;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using SharpScss;
using Zio;

namespace Lunet.Scss
{
    public class ScssProcessor : ContentProcessor<ScssPlugin>
    {
        public static readonly ContentType ScssType = new ContentType("scss");

        public ScssProcessor(ScssPlugin plugin) : base(plugin)
        {
        }

        public override ContentResult TryProcess(ContentObject file)
        {
            var contentType = file.ContentType;

            // This plugin is only working on scss files
            if (contentType != ScssType)
            {
                return ContentResult.None;
            }

            if (file.Content == null)
            {
                file.Content = file.SourceFile.ReadAllText();
            }

            var content = file.Content;

            var scss = (ScssObject)Site["scss"];

            var options = new ScssOptions();
            foreach (var pathObj in scss.Includes)
            {
                var path = pathObj as string;
                if (path != null)
                {
                    throw new NotImplementedException("Need rework with Zio");
                    //options.IncludePaths.Add((UPath)path);
                }
            }

            var result = SharpScss.Scss.ConvertToCss(content, options);

            file.Content = result.Css;
            file.ChangeContentType(ContentType.Css);

            if (result.IncludedFiles != null)
            {
                foreach (var includeFile in result.IncludedFiles)
                {
                    throw new NotImplementedException("Need rework with Zio");
                    file.Dependencies.Add(new FileContentDependency(new FileEntry(Site.FileSystem, (UPath)includeFile)));
                }
            }

            return ContentResult.Continue;
        }
    }
}