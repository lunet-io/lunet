// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using Lunet.Helpers;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Menus;

public class TestMenuAsyncPartialRendering
{
    [Test]
    public async Task TestBuildEmitsLargeSidebarMenuAsHashedPartial()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/config.scriban",
            """
            baseurl = "https://example.com"

            with menu
                async_load_threshold = 10
                async_partials_folder = "/partials/menus"
            end
            """);

        // Provide the async menu loader JS that would normally come from the Menus shared files.
        context.WriteAllText("/site/modules/menus/lunet-menu-async.js", "/* test stub */");

        context.WriteAllText("/site/menu.yml",
            """
            home:
              - {path: readme.md, title: "Home"}
              - {path: docs/readme.md, title: "Docs", folder: true}
            """);

        context.WriteAllText("/site/docs/menu.yml",
            """
            doc:
              - {path: readme.md, title: "Docs home"}
              - {path: p01.md, title: "Page 01"}
              - {path: p02.md, title: "Page 02"}
              - {path: p03.md, title: "Page 03"}
              - {path: p04.md, title: "Page 04"}
              - {path: p05.md, title: "Page 05"}
              - {path: p06.md, title: "Page 06"}
              - {path: p07.md, title: "Page 07"}
              - {path: p08.md, title: "Page 08"}
              - {path: p09.md, title: "Page 09"}
              - {path: p10.md, title: "Page 10"}
              - {path: p11.md, title: "Page 11"}
              - {path: p12.md, title: "Page 12"}
            """);

        context.WriteAllText("/site/.lunet/layouts/default.sbn-html",
            """
            ---
            layout: _default
            ---
            {{~ menu = page.menu ~}}
            {{~ if menu != null ~}}
            {{ menu.render { kind: "menu", collapsible: true, depth: 6 } }}
            {{~ end ~}}
            """);

        context.WriteAllText("/site/readme.md",
            """
            ---
            layout: default
            ---
            # Home
            """);

        context.WriteAllText("/site/docs/readme.md",
            """
            ---
            layout: default
            ---
            # Docs
            """);

        for (var i = 1; i <= 12; i++)
        {
            context.WriteAllText($"/site/docs/p{i:00}.md",
                $"""
                ---
                layout: default
                ---
                # Page {i:00}
                """);
        }

        var exitCode = await context.RunAsync("--input-dir=site", "build", "--dev");

        Assert.AreEqual(0, exitCode);

        var docsPageHtml = context.ReadAllText("/site/.lunet/build/www/docs/p12/index.html");
        Assert.IsTrue(docsPageHtml.Contains("data-lunet-menu-partial=", StringComparison.Ordinal));
        Assert.IsTrue(docsPageHtml.Contains("menu-loading", StringComparison.Ordinal));

        var match = Regex.Match(docsPageHtml, "data-lunet-menu-partial='([^']+)'");
        Assert.IsTrue(match.Success);
        var partialUrl = match.Groups[1].Value;
        Assert.IsTrue(partialUrl.StartsWith("/partials/menus/menu-doc.", StringComparison.Ordinal));

        var partialPath = "/site/.lunet/build/www" + partialUrl;
        Assert.IsTrue(context.FileExists(partialPath));

        var partialHtml = context.ReadAllText(partialPath);
        var expectedHash = HashUtil.HashStringHex(partialHtml);
        Assert.IsTrue(partialUrl.Contains($".{expectedHash}.html", StringComparison.Ordinal));
    }
}
