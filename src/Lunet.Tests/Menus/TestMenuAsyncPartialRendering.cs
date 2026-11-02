// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
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

    [Test]
    public async Task TestAsyncMenuActiveIdsMatchPartialWithFolderOverviews()
    {
        var context = new LunetAppTestContext();
        context.WriteAllText("/site/config.scriban",
            """
            baseurl = "https://example.com"

            with menu
                async_load_threshold = 5
                async_partials_folder = "/partials/menus"
            end
            """);

        context.WriteAllText("/site/modules/menus/lunet-menu-async.js", "/* test stub */");

        // The folder items have a sub-menu whose first item is the folder page itself (an overview page):
        // it is not rendered as a child of the folder item, so it must not shift the ids of the following items.
        context.WriteAllText("/site/docs/menu.yml",
            """
            doc:
              - {path: readme.md, title: "Docs home"}
              - {path: p01.md, title: "Page 01"}
              - {path: guide/readme.md, title: "Guide", folder: true}
              - {path: p02.md, title: "Page 02"}
              - {path: other/readme.md, title: "Other", folder: true}
              - {path: p03.md, title: "Page 03"}
              - {path: p04.md, title: "Page 04"}
            """);

        context.WriteAllText("/site/docs/guide/menu.yml",
            """
            guide:
              - {path: readme.md, title: "Guide overview"}
              - {path: g1.md, title: "Guide 1"}
              - {path: g2.md, title: "Guide 2"}
              - {path: g3.md, title: "Guide 3"}
              - {path: g4.md, title: "Guide 4"}
            """);

        context.WriteAllText("/site/docs/other/menu.yml",
            """
            other:
              - {path: readme.md, title: "Other overview"}
              - {path: o1.md, title: "Other 1"}
            """);

        // 1) the full doc menu, 2) the doc menu capped to its first level, 3) the folder menu of the page (guide pages only)
        context.WriteAllText("/site/.lunet/layouts/default.sbn-html",
            """
            ---
            layout: _default
            ---
            {{ site.menu.doc.render { kind: "menu", collapsible: true, depth: 6 } }}
            {{ site.menu.doc.render { kind: "menu", collapsible: true, depth: 1 } }}
            {{~ if page.url | string.contains "/guide/" ~}}
            {{ page.menu.render { kind: "menu", collapsible: true, depth: 6 } }}
            {{~ end ~}}
            """);

        var pages = new[] { "readme", "p01", "p02", "p03", "p04", "guide/readme", "guide/g1", "guide/g2", "guide/g3", "guide/g4", "other/readme", "other/o1" };
        foreach (var name in pages)
        {
            context.WriteAllText($"/site/docs/{name}.md",
                $"""
                ---
                layout: default
                ---
                # {name}
                """);
        }

        var exitCode = await context.RunAsync("--input-dir=site", "build", "--dev");
        Assert.AreEqual(0, exitCode);

        // Pages after the folders: the active item is the item of the page
        AssertAsyncMenu(context, "/docs/p02/", 0, ["/docs/p02/"]);
        AssertAsyncMenu(context, "/docs/p03/", 0, ["/docs/p03/"]);
        AssertAsyncMenu(context, "/docs/p04/", 0, ["/docs/p04/"]);
        AssertAsyncMenu(context, "/docs/p04/", 1, ["/docs/p04/"]);

        // Folder overview pages highlight their folder item
        AssertAsyncMenu(context, "/docs/guide/", 0, ["/docs/guide/"]);
        AssertAsyncMenu(context, "/docs/other/", 0, ["/docs/other/"]);
        AssertAsyncMenu(context, "/docs/other/", 1, ["/docs/other/"]);

        // Pages in a folder highlight their item and the folder item
        AssertAsyncMenu(context, "/docs/guide/g2/", 0, ["/docs/guide/g2/", "/docs/guide/"]);
        AssertAsyncMenu(context, "/docs/other/o1/", 0, ["/docs/other/o1/", "/docs/other/"]);

        // Beyond the depth of the menu, the folder item is highlighted
        AssertAsyncMenu(context, "/docs/guide/g2/", 1, ["/docs/guide/"]);

        // In the folder menu, the root has no item: the overview item stands for it, only for the overview page
        AssertAsyncMenu(context, "/docs/guide/", 2, ["/docs/guide/"]);
        AssertAsyncMenu(context, "/docs/guide/g3/", 2, ["/docs/guide/g3/"]);
    }

    private static void AssertAsyncMenu(LunetAppTestContext context, string pageUrl, int menuIndex, string[] expectedActiveUrls)
    {
        var pageHtml = context.ReadAllText($"/site/.lunet/build/www{pageUrl}index.html");
        var shells = Regex.Matches(pageHtml, "<div class='lunet-menu-async' data-lunet-menu-partial='([^']+)' data-lunet-menu-open='([^']*)'(?: data-lunet-menu-active='([^']*)')?>");
        Assert.Greater(shells.Count, menuIndex, $"Missing async menu #{menuIndex} in page {pageUrl}");
        var shell = shells[menuIndex];

        var partialHtml = context.ReadAllText("/site/.lunet/build/www" + shell.Groups[1].Value);
        foreach (var openId in shell.Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.IsTrue(partialHtml.Contains($"<ol id='{openId}'", StringComparison.Ordinal), $"Open id `{openId}` of page {pageUrl} has no list in the partial");
        }

        var activeUrls = new List<string>();
        foreach (var activeId in shell.Groups[3].Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var item = Regex.Match(partialHtml, $"<li id='{Regex.Escape(activeId)}'[^>]*>\\s*<span[^>]*><a href='([^']*)'");
            Assert.IsTrue(item.Success, $"Active id `{activeId}` of page {pageUrl} has no item in the partial");
            activeUrls.Add(item.Groups[1].Value);
        }

        Assert.AreEqual(string.Join(", ", expectedActiveUrls), string.Join(", ", activeUrls), $"Active items of menu #{menuIndex} in page {pageUrl}");
    }
}
