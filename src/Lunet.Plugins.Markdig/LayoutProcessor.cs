// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Textamina.Markdig;
using Textamina.Scriban;
using Textamina.Scriban.Runtime;

namespace Lunet.Plugins.Markdig
{
    /*
    public class LayoutProcessor : PageProcessorBase
    {
        private readonly Dictionary<string, ScriptPage> layouts;

        public LayoutProcessor()
        {
            layouts = new Dictionary<string, ScriptPage>();
        }

        public override bool CanProcess(string extension)
        {
            return extension == ".md" || extension == ".markdown";
        }

        public override bool Process(IFileObject file)
        {
            var page = (PageObject)file;
            var local = new ScriptObject();
            Context.PushGlobal(local);
            local.SetValue(PageVariables.Page, page, true);
            page.Script.Evaluate(Context);
            Context.PopGlobal();

            var source = Context.Output.ToString();
            Context.Output.Clear();
            var htmlOutput = Markdown.ToHtml(source);

            var layout = GetLayout(page);

            local = new ScriptObject();
            Context.PushGlobal(local);
            local.SetValue(PageVariables.Page, page, true);
            local.SetValue(PageVariables.Site, this, true);
            Context.SetValue(ScriptVariable.BlockDelegate, htmlOutput);
            layout.Evaluate(Context);
            Context.PopGlobal();

            page.Output = Context.Output.ToString();
            ProcessOutput(page);

            return true;
        }

        private void ProcessOutput(PageObject page)
        {
            var outputFile = Path.ChangeExtension(Path.Combine(Path.Combine(Site.BaseDirectoryInfo, "_site"), page.Path), ".html");

            var outputDir = Path.GetDirectoryName(outputFile);

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.WriteAllText(outputFile, page.Output, Encoding.UTF8);
        }


        private ScriptPage GetLayout(PageObject page)
        {
            var layoutName = "default.html";

            ScriptPage layoutPage = null;

            if (!layouts.TryGetValue(layoutName, out layoutPage))
            {
                var defaultLayout = Path.Combine(Site.BaseDirectoryInfo, "_meta/layouts/default.html");
                var template = Template.Parse(File.ReadAllText(defaultLayout), defaultLayout);

                if (template.HasErrors)
                {
                    // TODO: errors
                }

                // TODO: handle layout
                layoutPage = template.Page;

                layouts.Add(layoutName, layoutPage);
            }

            return layoutPage;
        }
    }
    */
}