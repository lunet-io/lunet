using System.Collections;
using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using SharpScss;

namespace Lunet.Plugins.SharpScss
{
    public class ScssProcessor : ContentProcessor
    {
        public static readonly ContentType ScssType = new ContentType("scss");

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
                file.Content = File.ReadAllText(file.SourceFile);
            }

            var content = file.Content;

            var scss = (ScssObject)Site.DynamicObject["scss"];

            var options = new ScssOptions();
            foreach (var pathObj in scss.Includes)
            {
                var path = pathObj as string;
                if (path != null)
                {
                    path = PathUtil.NormalizeRelativePath(path, true);
                    var includeDir = Site.BaseDirectory.Combine(path);
                    options.IncludePaths.Add(includeDir);
                }
            }

            var result = Scss.ConvertToCss(content, options);

            file.Content = result.Css;
            file.ChangeContentType(ContentType.Css);

            return ContentResult.Continue;
        }
    }
}