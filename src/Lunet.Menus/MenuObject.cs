// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Lunet.Core;
using Lunet.Helpers;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
using Zio;

namespace Lunet.Menus;

/// <summary>
/// A menu
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class MenuObject : DynamicObject
{
    private const string AsyncStateKeyPrefix = "__menu_async_state:";

    private enum MenuRenderMode
    {
        PageSpecific,
        Static,
    }

    private sealed record MenuNodeIds(string ListId, string ItemId);

    private sealed record MenuAsyncState(string PartialPath, Dictionary<MenuObject, MenuNodeIds> NodeIds, string RootListId);

    public MenuObject()
    {
        Children = new MenuCollection();
        InitializeBuiltins();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{(Url != null ? $"Url: {Url}" : $"Path: {Path}")} Name: {Name}, Parent: {Parent?.Name}, Children = {Children.Count}";

    public string? Name
    {
        get => GetSafeValue<string>("name");
        set => SetValue("name", value, true);
    }
        
    public string? Path
    {
        get => GetSafeValue<string>("path");
        set => SetValue("path", value, true);
    }
       
    public string? Title
    {
        get => GetSafeValue<string>("title");
        set => SetValue("title", value);
    }
        
    public string? Pre
    {
        get => GetSafeValue<string>("pre");
        set => SetValue("pre", value);
    }
        
    public string? Post
    {
        get => GetSafeValue<string>("post");
        set => SetValue("post", value);
    }

    public string? Url
    {
        get => GetSafeValue<string>("url");
        set => SetValue("url", value);
    }

    public string? Target
    {
        get => GetSafeValue<string>("target");
        set => SetValue("target", value);
    }

    public bool Folder
    {
        get => GetSafeValue<bool>("folder");
        set => SetValue("folder", value);
    }

    public bool Separator
    {
        get => GetSafeValue<bool>("separator");
        set => SetValue("separator", value);
    }

    public bool Generated
    {
        get => GetSafeValue<bool>("generated");
        set => SetValue("generated", value);
    }

    public int Width
    {
        get
        {
            var width = GetSafeValue<int>("width");
            if (width == 0)
            {
                width = 3;
            }
            return Math.Clamp(width, 2, 4);
        }
        set => SetValue("width", Math.Clamp(value, 2, 4), true);
    }

    public MenuCollection Children { get; }

    public MenuObject? Parent
    {
        get => GetSafeValue<MenuObject>("parent");
        set => SetValue("parent", value, true);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        // As it is a recursive structure, we protect against recursive ToString() calls
        return $"Menu: {DebuggerDisplay}";
    }

    public ContentObject? Page
    {
        get => GetSafeValue<ContentObject>("page");
        set => SetValue("page", value);
    }

    public bool HasChildren() => this.Count > 0;
        
    private void InitializeBuiltins()
    {
        this.SetValue("has_children", DelegateCustomFunction.CreateFunc(HasChildren), true);
        this.SetValue("children", Children, true);
        this.Import("render", (Func<TemplateContext, SourceSpan, ScriptObject?, string>)(Render));
        this.Import("breadcrumb", (Func<TemplateContext, SourceSpan, ScriptObject?, string>)(RenderBreadcrumb));
    }

    public string Render(TemplateContext context, SourceSpan span, ScriptObject? options = null)
    {
        var builder = new StringBuilder();
        var effectiveOptions = options ?? new ScriptObject();

        if (!context.CurrentGlobal.TryGetValue(context, span, "page", out var pageObject) || pageObject is not ContentObject page)
        {
            throw new ScriptRuntimeException(span, "Invalid usage of menu. There is no active page.");
        }

        var kind = effectiveOptions["kind"]?.ToString() ?? "menu";
        var showCollapse = effectiveOptions["collapsible"] is bool v && v;
        var maxDepth = GetDepth(effectiveOptions);
        var allowAsync = effectiveOptions.TryGetValue("async", out var asyncObj) ? asyncObj as bool? ?? true : true;

        if (allowAsync && kind == "menu")
        {
            var plugin = page.Site.GetSafeValue<MenuPlugin>("menu");
            if (plugin is not null)
            {
                var threshold = plugin.AsyncLoadThreshold;
                if (threshold > 0 && CountItems() >= threshold)
                {
                    return RenderAsync(page, effectiveOptions, plugin, kind, maxDepth, showCollapse);
                }
            }
        }

        int index = 0;
        RenderInternal(page, builder, 0, effectiveOptions, null, this, ref index, MenuRenderMode.PageSpecific);
        return builder.ToString();
    }
        
    public string RenderBreadcrumb(TemplateContext context, SourceSpan span, ScriptObject? options = null)
    {
        var builder = new StringBuilder();
        if (!context.CurrentGlobal.TryGetValue(context, span, "page", out var pageObject) || !(pageObject is ContentObject))
        {
            throw new ScriptRuntimeException(span, "Invalid usage of menu. There is no active page.");
        }

        RenderBreadcrumb((ContentObject) pageObject, builder, options ?? new ScriptObject());
        return builder.ToString();
    }

    private static int GetDepth(ScriptObject options)
    {
        int maxDepth = int.MaxValue;
        if (options.TryGetValue("depth", out var maxDepthObj) && maxDepthObj is int tempMaxDepth)
        {
            maxDepth = tempMaxDepth;
        }
        return maxDepth;
    }

    private int CountItems()
    {
        var total = 0;
        foreach (var child in Children)
        {
            total += CountItemsRecursive(child);
        }
        return total;
    }

    private static int CountItemsRecursive(MenuObject menu)
    {
        var total = 1;
        foreach (var child in menu.Children)
        {
            total += CountItemsRecursive(child);
        }
        return total;
    }

    private static string NormalizeAsyncFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return "/partials/menus";
        }

        var normalized = folder.Replace('\\', '/').Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }
        return normalized.TrimEnd('/');
    }

    private static string MakeSafeFileStem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "menu";
        }

        var text = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                builder.Append(ch);
            }
            else if (ch == '_' || ch == '-' || ch == '.' || ch == ' ')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(result) ? "menu" : result;
    }

    private static string GetAsyncStateKey(string kind, int maxDepth, bool showCollapse, ScriptObject options)
    {
        static string AsString(ScriptObject opts, string key)
        {
            return opts.TryGetValue(key, out var value) && value is not null ? value.ToString() ?? string.Empty : string.Empty;
        }

        return string.Join("|",
            kind,
            showCollapse ? "c1" : "c0",
            maxDepth.ToString(CultureInfo.InvariantCulture),
            AsString(options, "list_class"),
            AsString(options, "list_item_class"),
            AsString(options, "link_class"),
            AsString(options, "link_args"));
    }

    private static Dictionary<MenuObject, MenuNodeIds> BuildNodeIds(MenuObject root, string kind)
    {
        var rootName = root.Name ?? "menu";
        int index = 0;
        var map = new Dictionary<MenuObject, MenuNodeIds>(ReferenceEqualityComparer.Instance);
        BuildNodeIds(root, kind, rootName, ref index, map);
        return map;
    }

    private static void BuildNodeIds(MenuObject node, string kind, string rootName, ref int index, Dictionary<MenuObject, MenuNodeIds> map)
    {
        var listId = $"{kind}-id-{rootName}-{index}";
        var itemId = listId.Replace($"{kind}-id-", $"{kind}-item-", StringComparison.Ordinal);
        map[node] = new MenuNodeIds(listId, itemId);

        foreach (var child in node.Children)
        {
            index++;
            BuildNodeIds(child, kind, rootName, ref index, map);
        }
    }

    private string RenderAsync(ContentObject page, ScriptObject options, MenuPlugin plugin, string kind, int maxDepth, bool showCollapse)
    {
        var stateKey = AsyncStateKeyPrefix + GetAsyncStateKey(kind, maxDepth, showCollapse, options);
        MenuAsyncState state;
        lock (this)
        {
            if (ContainsKey(stateKey) && this[stateKey] is MenuAsyncState existingState)
            {
                state = existingState;
            }
            else
            {
                state = CreateAsyncState(page, options, plugin, kind, maxDepth, showCollapse);
                SetValue(stateKey, state, true);
            }
        }

        var partialUrl = page.Site.Content.Finder.UrlRelRef(page, state.PartialPath);

        // Active/open ids are page-specific but tiny compared to the menu HTML.
        var openIds = new List<string> { state.RootListId };
        var activeItemIds = new List<string>();

        if (page.GetSafeValue<MenuObject>("menu_item") is { } currentItem)
        {
            var node = currentItem;
            while (node is not null)
            {
                if (state.NodeIds.TryGetValue(node, out var ids))
                {
                    activeItemIds.Add(ids.ItemId);
                    if (node.Children.Count > 0)
                    {
                        openIds.Add(ids.ListId);
                    }
                }
                node = node.Parent;
            }
        }

        var openAttr = string.Join(",", openIds.Distinct());
        var activeAttr = string.Join(",", activeItemIds.Distinct());

        var shell = new StringBuilder();
        shell.Append($"<div class='lunet-menu-async' data-lunet-menu-partial='{partialUrl}' data-lunet-menu-open='{openAttr}'");
        if (!string.IsNullOrEmpty(activeAttr))
        {
            shell.Append($" data-lunet-menu-active='{activeAttr}'");
        }
        shell.Append('>');

        // Minimal fallback for no-JS / failed fetch: keep the sidebar lightweight.
        shell.Append("<div class='menu-loading text-body-secondary small'>Loading menu\u2026</div>");

        shell.Append("</div>");
        return shell.ToString();
    }

    private MenuAsyncState CreateAsyncState(ContentObject page, ScriptObject options, MenuPlugin plugin, string kind, int maxDepth, bool showCollapse)
    {
        var nodeIds = BuildNodeIds(this, kind);
        var rootListId = nodeIds.TryGetValue(this, out var rootIds) ? rootIds.ListId : $"{kind}-id-{Name ?? "menu"}-0";

        var partialBuilder = new StringBuilder();
        int index = 0;
        RenderInternal(page, partialBuilder, 0, options, null, this, ref index, MenuRenderMode.Static);
        var html = partialBuilder.ToString();
        var hash = HashUtil.HashStringHex(html);

        var folder = NormalizeAsyncFolder(plugin.AsyncPartialsFolder);
        var stem = MakeSafeFileStem(Name);
        var partialPath = $"{folder}/menu-{stem}.{hash}.html";
        var outputPath = (UPath)partialPath;

        // Defer writing to a single, centralized processor (AfterProcessing) to avoid concurrency issues.
        var map = page.Site.GetSafeValue<ConcurrentDictionary<UPath, string>>(MenuAsyncPartialsWriter.SiteKey);
        if (map is null)
        {
            map = new ConcurrentDictionary<UPath, string>();
            page.Site.SetValue(MenuAsyncPartialsWriter.SiteKey, map, true);
        }
        map.TryAdd(outputPath, html);

        return new MenuAsyncState(partialPath, nodeIds, rootListId);
    }
        
    private const int IndentSize = 2;
        
       
    private void RenderInternal(ContentObject page, StringBuilder builder, int level, ScriptObject options, MenuObject? parent, MenuObject root, ref int index, MenuRenderMode mode)
    {
        // Don't process further if we are only looking at a certain level
        int maxDepth = int.MaxValue;
        if (options.TryGetValue("depth", out var maxDepthObj) && maxDepthObj is int tempMaxDepth)
        {
            maxDepth = tempMaxDepth;
        }

        var kind = options["kind"]?.ToString() ?? "menu";
        var showCollapse = options["collapsible"] is bool v && v;
            
        bool hasChildren = Children.Count > 0 && this != parent; // Don't render recursively
        string menuId = $"{kind}-id-{root.Name ?? "menu"}-{index}";

        bool isCurrentPageInMenuPath = false;
        if (mode == MenuRenderMode.PageSpecific)
        {
            var currentMenu = page.GetSafeValue<MenuObject>("menu_item");
            while (currentMenu != null)
            {
                if (ReferenceEquals(currentMenu, this))
                {
                    isCurrentPageInMenuPath = true;
                    break;
                }
                currentMenu = currentMenu.Parent;
            }
        }

        if (level > 0)
        {
            RenderItem(page, builder, level, kind, options, level >= maxDepth, hasChildren, menuId, isCurrentPageInMenuPath, showCollapse, mode);
        }

        if (hasChildren && level < maxDepth) 
        {
            builder.Append(' ', level * IndentSize);
            var listKind = (kind == "nav" ? "navbar-nav" : kind);
            var collapse = level > 0 && showCollapse
                ? (isCurrentPageInMenuPath ? "collapse show" : "collapse")
                : "show";
            builder.AppendLine($"<ol id='{menuId}' class='{listKind} {kind}-level{level} {collapse} {options["list_class"]}'>");
            foreach (var item in this.Children)
            {
                if (level > 0 && item.Path == this.Path)
                {
                    // Avoid showing the same menu item as a child of itself, e.g. when put as a first child
                    continue;
                }

                index++;
                item.RenderInternal(page, builder, level + 1, options, this, root, ref index, mode);
            }

            builder.Append(' ', level * IndentSize);
            builder.AppendLine("</ol>");
        }

        if (level > 0)
        {
            builder.Append(' ', level * IndentSize);
            builder.AppendLine("</li>");
        }
    }

    private bool RenderItem(ContentObject page, StringBuilder builder, int level, string kind, ScriptObject options, bool isCappingDepth, bool hasChildren, string? subMenuId, bool isInCurrentPath, bool showCollapse, MenuRenderMode mode)
    {
        builder.Append(' ', level * IndentSize);

        bool isActive = false;
        if (mode == MenuRenderMode.PageSpecific)
        {
            var currentMenu = page.GetSafeValue<MenuObject>("menu_item");
            while (currentMenu != null)
            {
                if (ReferenceEquals(currentMenu, this))
                {
                    isActive = true;
                    break;
                }

                if (!isCappingDepth)
                {
                    break;
                }

                currentMenu = currentMenu.Parent;
            }

            // If the page is not in a menu, try to recover it from the same section
            if (!string.IsNullOrEmpty(page.Section) && page.GetSafeValue<MenuObject>("menu_item") == null && page.Section == Page?.Section)
            {
                isActive = true;
            }
        }
            
        bool isBreadcrumb = kind == "breadcrumb";

        var thisLinkItemClass = this["list_item_class"];
        thisLinkItemClass = thisLinkItemClass != null ? $" {thisLinkItemClass}" : string.Empty;
            
        var itemId = !isBreadcrumb && !string.IsNullOrEmpty(subMenuId)
            ? subMenuId.Replace($"{kind}-id-", $"{kind}-item-", StringComparison.Ordinal)
            : null;

        builder.AppendLine($"<li{(itemId != null ? $" id='{itemId}'" : string.Empty)} class='{kind}-item {options["list_item_class"]}{(isActive ? " active" : string.Empty)}{thisLinkItemClass}'>");

        builder.Append(' ', (level + 1) * IndentSize);

        if (page == Page)
        {
            builder.Append(options["pre_active"]);
        }

        if (!isBreadcrumb)
        {
            builder.Append($"<span class='{kind}-item-row'>");
        }

        var linkClassFromOptions = (isActive ? options?["link_class_active"] : null) ?? options?["link_class"];
        var linkClassFromMenu = (isActive ? this["link_class_active"] : null) ?? this["link_class"];
        var linkArgsFromOptions = ((isActive ? options?["link_args_active"] : null) ?? options?["link_args"]) ?? string.Empty;
        var linkArgsFromMenu = ((isActive ? this["link_args_active"] : null) ?? this["link_args"]) ?? string.Empty;
        var title = Title ?? Page?.Title;
        var isSeparator = Separator;
        if (isSeparator)
        {
            builder.Append($"<span class='{kind}-separator {linkClassFromOptions} {linkClassFromMenu}'{linkArgsFromOptions}{linkArgsFromMenu}>");
        }
        else if (!isActive || !isBreadcrumb)
        {
            var url = (Url ?? Page?.Url ?? "#");

            // If this is the top menu file, we link to the base path
            if (url == $"/{MenuProcessor.MenuFileName}")
            {
                url = $"{page.Site.BasePath}/";
            }

            if (url.StartsWith("xref:"))
            {
                var uid = url.Substring("xref:".Length);
                url = page.Site.Content.Finder.UrlRelRef(page, url);
                if (page.Site.Content.Finder.TryFindByUid(uid, out var pageContent))
                {
                    title ??= pageContent.Title;
                }
            }

            builder.Append($"<a href='{url}'{(Target != null ? $" target='{Target}'" : string.Empty)} class='{kind}-link {linkClassFromOptions} {linkClassFromMenu}'{linkArgsFromOptions}{linkArgsFromMenu}>");
        }

        // Append the title
        builder.Append($"{Pre}{title}{Post}");

        if (isSeparator)
        {
            builder.Append($"</span>");
        }
        else if (!isActive || !isBreadcrumb)
        {
            builder.Append($"</a>");
        }

        if (hasChildren && showCollapse)
        {
            builder.Append(@$"<a href='#{subMenuId}' role='button' data-bs-toggle='collapse' aria-expanded='{(isInCurrentPath ? "true" : "false")}' aria-controls='{subMenuId}' class='{kind}-link-show{(isInCurrentPath ? "" : " collapsed")}'></a>");
        }

        if (!isBreadcrumb)
        {
            builder.Append("</span>");
        }

        if (page == Page)
        {
            if (options.TryGetValue("post_active", out var postActive) && postActive is not null)
            {
                builder.Append(postActive);
            }
        }

        return isActive;
    }

    private void RenderBreadcrumb(ContentObject page, StringBuilder builder, ScriptObject options)
    {
        var menus = new Stack<MenuObject>();
        var menu = page.GetSafeValue<MenuObject>("menu_item");

        ContentObject? previousPage = null;
        while (menu != null)
        {
            // Because the same page can be declared
            // as a a menu and (usually the first) submenu-item
            // we filter that case here to print only the page once
            if (menu.Page != previousPage)
            {
                menus.Push(menu);
            }
            previousPage = menu.Page;
            menu = menu.Parent;
        }

        if (menus.Count > 0)
        {
            builder.AppendLine($"<ol class='breadcrumb'>");
            foreach (var item in menus)
            {
                item.RenderItem(page, builder, 1, "breadcrumb", options, false, false, null, true, false, MenuRenderMode.PageSpecific);
                builder.Append(' ', 1 * IndentSize);
                builder.AppendLine("</li>");
            }
            builder.AppendLine("</ol>");
        }
    }
}
