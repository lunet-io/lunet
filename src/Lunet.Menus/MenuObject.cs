// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class MenuObject : DynamicCollection<MenuObject, MenuObject>
    {
        public MenuObject()
        {
            InitializeBuiltins();
        }

        public MenuObject(IEnumerable<MenuObject> values) : base(values)
        {
            InitializeBuiltins();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Menu: {Menu}, Name: {Name}, Parent: {Parent}, Children = {Count}";

        public string Name
        {
            get => GetSafeValue<string>("name");
            set => SetValue("name", value, true);
        }
        
        public string Key
        {
            get => GetSafeValue<string>("key");
            set => SetValue("key", value);
        }
        
        public string Title
        {
            get => GetSafeValue<string>("title");
            set => SetValue("title", value);
        }

        public string Menu
        {
            get => GetSafeValue<string>("menu");
            set => SetValue("menu", value);
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

        public int? Weight
        {
            get => GetSafeValue<object>("weight") is int tvalue ? (int?) tvalue : null;
            set => SetValue("weight", value.HasValue ? (object)value.Value : null);
        }

        public MenuObject Parent
        {
            get => GetSafeValue<MenuObject>("parent");
            set => SetValue("parent", value, true);
        }

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
        
        protected override IEnumerable<MenuObject> OrderByDefault()
        {
            return OrderByWeight();
        }
        
        public MenuObject OrderByWeight()
        {
            return new MenuObject(this.OrderBy(o => o.Weight ?? o.Page?.Weight ?? 0));
        }

        public bool HasChildren() => this.Count > 0;
        
        protected override MenuObject Clone()
        {
            var menuObject = new MenuObject
            {
                Name = Name,
                Weight = Weight,
                Parent = Parent,
                Page = Page,
            };
            foreach (var subItem in this)
            {
                menuObject.Add(subItem);
            }
            return menuObject;
        }
        
        private void InitializeBuiltins()
        {
            this.ScriptObject.Import("by_weight", (OrderDelegate)OrderByWeight);
            this.ScriptObject.SetValue("has_children", DelegateCustomFunction.CreateFunc(HasChildren), true);
            this.ScriptObject.Import("render", (Func<TemplateContext, SourceSpan, string>)(Render));
            this.ScriptObject.Import("breadcrumb", (Func<TemplateContext, SourceSpan, string>)(RenderBreadcrumb));
        }

        public string Render(TemplateContext context, SourceSpan span)
        {
            var builder = new StringBuilder();
            if (!context.CurrentGlobal.TryGetValue(context, span, "page", out var pageObject) || !(pageObject is ContentObject))
            {
                throw new ScriptRuntimeException(span, "Invalid usage of menu. There is no active page.");
            }

            Render((ContentObject) pageObject, builder, 0);
            return builder.ToString();
        }
        
        public string RenderBreadcrumb(TemplateContext context, SourceSpan span)
        {
            var builder = new StringBuilder();
            if (!context.CurrentGlobal.TryGetValue(context, span, "page", out var pageObject) || !(pageObject is ContentObject))
            {
                throw new ScriptRuntimeException(span, "Invalid usage of menu. There is no active page.");
            }

            RenderBreadcrumb((ContentObject) pageObject, builder);
            return builder.ToString();
        }
        
        private const int IndentSize = 2;
        
       
        private void Render(ContentObject page, StringBuilder builder, int level)
        {
            if (level > 0)
            {
                RenderItem(page, builder, level, "menu-item");
            }

            if (this.Count > 0)
            {
                builder.Append(' ', level * IndentSize);
                builder.AppendLine($"<ul class='{(level == 0 ? "menu" : $"submenu submenu-{level}")}'>");
                foreach (var item in this.OrderByWeight())
                {
                    item.Render(page, builder, level + 1);
                }

                builder.Append(' ', level * IndentSize);
                builder.AppendLine("</ul>");
            }

            if (level > 0)
            {
                builder.Append(' ', level * IndentSize);
                builder.AppendLine("</li>");
            }
        }

        private void RenderItem(ContentObject page, StringBuilder builder, int level, string classKind)
        {
            builder.Append(' ', level * IndentSize);
            builder.AppendLine($"<li class='{classKind}{(Page == page ? " active" : string.Empty)}'>");

            builder.Append(' ', (level + 1) * IndentSize);
            if (page != Page)
            {
                builder.Append($"<a href='{(Url ?? Page?.Url ?? "#")}'{(Target != null ? $" target='{Target}'" : string.Empty)}>");
            }
            
            builder.Append($"{Pre}<span>{Title ?? Page?.Title}</span>{Post}");
            
            if (page != Page)
            {
                builder.Append("</a>");
            }
        }
        
        private void RenderBreadcrumb(ContentObject page, StringBuilder builder)
        {
            var menus = new Stack<MenuObject>();
            var menu = page.GetSafeValue<MenuObject>("menu");

            while (menu != null)
            {
                menus.Push(menu);
                menu = menu.Parent;
            }
            builder.AppendLine($"<ol class='breadcrumb'>");
            foreach (var item in menus)
            {
                item.RenderItem(page, builder, 1, "breadcrumb-item");
                builder.Append(' ', 1 * IndentSize);
                builder.AppendLine("</li>");
            }
            builder.AppendLine("</ol>");
        }

        private class DebuggerProxy
        {
            private readonly MenuObject _menu;

            public DebuggerProxy(MenuObject menu)
            {
                _menu = menu;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public MenuObject[] Items => _menu.ToArray();
        }
    }
}