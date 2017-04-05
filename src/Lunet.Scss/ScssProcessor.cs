using System.IO;
using Lunet.Core;
using Lunet.Helpers;
using SharpScss;

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
                file.Content = File.ReadAllText(file.SourceFile);
            }

            var content = file.Content;

            var scss = (ScssObject)Site["scss"];

            var options = new ScssOptions();
            foreach (var pathObj in scss.Includes)
            {
                var path = pathObj as string;
                if (path != null)
                {
                    path = PathUtil.NormalizeRelativePath(path, true);
                    var includeDir = Site.BaseFolder.Combine(path);
                    options.IncludePaths.Add(includeDir);
                }
            }

            var result = SharpScss.Scss.ConvertToCss(content, options);

            file.Content = result.Css;
            file.ChangeContentType(ContentType.Css);

            if (result.IncludedFiles != null)
            {
                foreach (var includeFile in result.IncludedFiles)
                {
                    file.Dependencies.Add(new FileContentDependency(includeFile));
                }
            }

            return ContentResult.Continue;
        }
    }
}