// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lunet.Core;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Lunet.Menus
{
    /// <summary>
    /// A menu
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class MenuObject : DynamicObject
    {
        public MenuObject()
        {
            Children = new MenuCollection();
            InitializeBuiltins();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"{(Url != null ? $"Url: {Url}" : $"Path: {Path}")} Name: {Name}, Parent: {Parent?.Name}, Children = {Children.Count}";

        public string Name
        {
            get => GetSafeValue<string>("name");
            set => SetValue("name", value, true);
        }
        
        public string Path
        {
            get => GetSafeValue<string>("path");
            set => SetValue("path", value, true);
        }
       
        public string Title
        {
            get => GetSafeValue<string>("title");
            set => SetValue("title", value);
        }
        
        public string Pre
        {
            get => GetSafeValue<string>("pre");
            set => SetValue("pre", value);
        }
        
        public string Post
        {
            get => GetSafeValue<string>("post");
            set => SetValue("post", value);
        }

        public string Url
        {
            get => GetSafeValue<string>("url");
            set => SetValue("url", value);
        }

        public string Target
        {
            get => GetSafeValue<string>("target");
            set => SetValue("target", value);
        }
        
        public MenuCollection Children { get; }

        public MenuObject Parent { get; set; }

        public string ParentAsString
        {
            get => GetSafeValue<string>("parent");
            set => SetValue("parent", value, true);
        }

        public ContentObject Page
        {
            get => GetSafeValue<ContentObject>("page");
            set => SetValue("page", value);
        }        

        public bool HasChildren() => this.Count > 0;
        
        private void InitializeBuiltins()
        {
            this.SetValue("has_children", DelegateCustomFunction.CreateFunc(HasChildren), true);
            this.SetValue("children", Children, true);
            this.Import("render", (Func<TemplateContext, SourceSpan, ScriptObject, string>)(Render));
            this.Import("breadcrumb", (Func<TemplateContext, SourceSpan, ScriptObject, string>)(RenderBreadcrumb));
        }

        public string Render(TemplateContext context, SourceSpan span, ScriptObject options = null)
        {
            var builder = new StringBuilder();

            if (!context.CurrentGlobal.TryGetValue(context, span, "page", out var pageObject) || !(pageObject is ContentObject))
            {
                throw new ScriptRuntimeException(span, "Invalid usage of menu. There is no active page.");
            }

            int index = 0;
            Render((ContentObject) pageObject, builder, 0, options ?? new ScriptObject(), null, this, ref index);
            return builder.ToString();
        }
        
        public string RenderBreadcrumb(TemplateContext context, SourceSpan span, ScriptObject options = null)
        {
            var builder = new StringBuilder();
            if (!context.CurrentGlobal.TryGetValue(context, span, "page", out var pageObject) || !(pageObject is ContentObject))
            {
                throw new ScriptRuntimeException(span, "Invalid usage of menu. There is no active page.");
            }

            RenderBreadcrumb((ContentObject) pageObject, builder, options ?? new ScriptObject());
            return builder.ToString();
        }
        
        private const int IndentSize = 2;
        
       
        private void Render(ContentObject page, StringBuilder builder, int level, ScriptObject options, MenuObject parent, MenuObject root, ref int index)
        {
            // Don't process further if we are only looking at a certain level
            int maxDepth = int.MaxValue;
            if (options.TryGetValue("depth", out var maxDepthObj) && maxDepthObj is int tempMaxDepth)
            {
                maxDepth = tempMaxDepth;
            }

            var kind = options["kind"]?.ToString() ?? "menu";
            var collapsible = options["collapsible"] is bool v && v;
            
            bool hasChildren = Children.Count > 0 && this != parent; // Don't render recursively
            string menuId = $"{kind}-id-{root.Name}-{index}";

            bool isCurrentPageInMenuPath = false;
            var currentMenu = page["menu"] as MenuObject;
            while (currentMenu != null)
            {
                if (currentMenu.Page == Page)
                {
                    isCurrentPageInMenuPath = true;
                    break;
                }
                currentMenu = currentMenu.Parent;
            }

            if (level > 0)
            {
                RenderItem(page, builder, level, kind, options, level >= maxDepth, hasChildren, menuId, isCurrentPageInMenuPath, collapsible);
            }

            if (hasChildren && level < maxDepth) 
            {
                builder.Append(' ', level * IndentSize);
                var listKind = (kind == "nav" ? "navbar-nav" : kind);
                builder.AppendLine($"<ol id='{menuId}' class='{listKind} {kind}-level{level} {(collapsible? $"collapse {(isCurrentPageInMenuPath ? " show" : string.Empty)}": string.Empty)} {options["list_class"]}'>");
                foreach (var item in this.Children)
                {
                    index++;
                    item.Render(page, builder, level + 1, options, this, root, ref index);
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

        private bool RenderItem(ContentObject page, StringBuilder builder, int level, string kind, ScriptObject options, bool isCappingDepth, bool hasChildren, string subMenuId, bool isInCurrentPath, bool showCollapse)
        {
            builder.Append(' ', level * IndentSize);

            var currentPage = page;
            bool isActive = false;
            while (currentPage != null)
            {
                if (currentPage == Page)
                {
                    isActive = true;
                }

                if (isCappingDepth && currentPage["menu"] is MenuObject menuObject)
                {
                    var nextPage = menuObject?.Parent?.Page;
                    // TODO: remove recursive
                    if (nextPage == currentPage) break;
                    currentPage = nextPage;
                }
                else
                {
                    break;
                }
            }

            bool isBreadcrumb = kind == "breadcrumb";

            var thisLinkItemClass = this["list_item_class"];
            thisLinkItemClass = thisLinkItemClass != null ? $" {thisLinkItemClass}" : string.Empty;
            
            builder.AppendLine($"<li class='{kind}-item {options["list_item_class"]}{(isActive ? " active" : string.Empty)}{thisLinkItemClass}'>");

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
            if (!isActive || !isBreadcrumb)
            {
                builder.Append($"<a href='{(Url ?? Page?.Url ?? "#")}'{(Target != null ? $" target='{Target}'" : string.Empty)} class='{kind}-link {linkClassFromOptions} {linkClassFromMenu}'>");
            }
            builder.Append($"{Pre}{Title ?? Page?.Title}{Post}");

            if (!isActive || !isBreadcrumb)
            {
                builder.Append($"</a>");
            }

            if (hasChildren && showCollapse)
            {
                builder.Append(@$"<a href='#{subMenuId}' role='button'  data-toggle='collapse' aria-expanded='{(isInCurrentPath ? "true" : "false")}' aria-controls='{subMenuId}' class='{kind}-link-show{(isInCurrentPath ? "" : " collapsed")}'></a>");
            }

            if (!isBreadcrumb)
            {
                builder.Append("</span>");
            }

            if (page == Page)
            {
                builder.Append(options["post_active"]);
            }



            return isActive;
        }

        private void RenderBreadcrumb(ContentObject page, StringBuilder builder, ScriptObject options)
        {
            var menus = new Stack<MenuObject>();
            var menu = page.GetSafeValue<MenuObject>("menu");

            ContentObject previousPage = null;
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
                    item.RenderItem(page, builder, 1, "breadcrumb", options, false, false, null, true, false);
                    builder.Append(' ', 1 * IndentSize);
                    builder.AppendLine("</li>");
                }
                builder.AppendLine("</ol>");
            }
        }
    }
}