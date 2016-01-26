using System.Collections.Generic;
using System.IO;
using System.Text;
using Textamina.Markdig;
using Textamina.Scriban.Runtime;

namespace Lunet.Plugins.Markdig
{
    public class MarkdigProcessor : PageProcessorBase
    {
        private readonly ScriptObject markdigOptions;

        public MarkdigProcessor()
        {
            markdigOptions = new ScriptObject();
        }

        public override bool CanProcess(string extension)
        {
            return extension == ".md" || extension == ".markdown";
        }

        public override void Initialize(SiteObject site)
        {
            base.Initialize(site);
            site.Plugins.SetValue("markdig", markdigOptions, true);
        }

        public override bool ProcessPage(PageObject page)
        {
            var pipeline = new MarkdownPipeline();

            if (markdigOptions.Count == 0)
            {
                pipeline.UseAllExtensions();
            }
            else
            {
                // TODO: handle Markdig options
            }

            var html = Markdown.ToHtml(page.Output, pipeline);
            page.Output = html;
            page.OutputExtension = ".html";

            return true;
        }
    }
}