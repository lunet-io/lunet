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

            // This plugin is only working on files with a frontmatter and the markdown extension
            if (contentType != ScssType)
            {
                return ContentResult.None;
            }

            if (file.Content == null)
            {
                file.Content = File.ReadAllText(file.SourceFile);
            }

            var content = file.Content;

            var scss = (DynamicObject) this.Site.Scripts.GlobalObject["scss"];

            var includePaths = scss["includes"] as IEnumerable;

            var options = new ScssOptions();
            if (includePaths != null)
            {
                foreach (var pathObj in includePaths)
                {
                    var path = pathObj as string;
                    if (path != null)
                    {
                        path = PathUtil.NormalizeRelativePath(path, true);
                        var includeDir = Site.BaseDirectory.Combine(path);
                        options.IncludePaths.Add(includeDir);
                    }
                }
            }

            var result = Scss.ConvertToCss(content, options);

            file.Content = result.Css;
            file.ChangeContentType(ContentType.Css);

            return ContentResult.Continue;
        }
    }
}