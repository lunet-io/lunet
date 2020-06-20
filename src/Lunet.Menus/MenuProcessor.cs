// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using Lunet.Core;
using Scriban.Runtime;

namespace Lunet.Menus
{
    public class MenuProcessor : ContentProcessor<MenuPlugin>
    {
        public MenuProcessor(MenuPlugin plugin) : base(plugin)
        {
        }

        public override ContentResult TryProcess(ContentObject page)
        {
            // Support only one menu string for now
            if (!page.TryGetValue("menu", out object o) || o == null)
            {
                return ContentResult.Continue;
            }

            ProcessMenu(o, page);

            return ContentResult.Continue;
        }

        private void SetPageMenu(ContentObject page, MenuObject menu)
        {
            page.SetValue("menu", menu, true);
        }
        
        private void ProcessMenu(object o, ContentObject page)
        {
            if (o is string)
            {
                 var menuName = (string) o;
                 var parentMenu = GetOrCreateMenu(menuName);
                 var menu = new MenuObject()
                 {
                     Menu = menuName,
                     Page = page,
                     Parent = parentMenu,
                 };
                 SetPageMenu(page, menu);
                 parentMenu.Add(menu);
            }
            else if (o is ScriptArray scriptArray)
            {
                foreach (var item in scriptArray)
                {
                    ProcessMenu(item, page);
                }
            }
            else if (o is IScriptObject scriptObject)
            {
                foreach (var menuName in scriptObject.GetMembers())
                {
                    var parentMenu = GetOrCreateMenu(menuName);
                    if (scriptObject.TryGetValue(menuName, out var objectValue))
                    {
                        if (objectValue is ScriptObject subScript)
                        {
                            var menu = new MenuObject()
                            {
                                Menu = menuName,
                                Page = page,
                            };
                            menu.ScriptObject.Import(subScript);

                            var localParentMenuName = menu.ParentAsString; 
                            if (localParentMenuName != null)
                            {
                                var localParentMenu = parentMenu.GetSafeValue<MenuObject>(localParentMenuName);
                                if (localParentMenu == null)
                                {
                                    localParentMenu = new MenuObject() {Name = localParentMenuName, Menu = parentMenu.Name};
                                    parentMenu.SetValue(localParentMenuName, localParentMenu);
                                    parentMenu.Add(localParentMenu);
                                }
                                menu.Parent = localParentMenu;
                                SetPageMenu(page, menu);
                                localParentMenu.Add(menu);
                            }
                            else
                            {
                                var subMenuName = menu.Name;
                                if (subMenuName != null)
                                {
                                    var subMenu = parentMenu.GetSafeValue<MenuObject>(subMenuName);
                                    if (subMenu != null)
                                    {
                                        subMenu.Menu = menuName;
                                        subMenu.Page = page;
                                        subMenu.ScriptObject.Import(subScript);
                                        SetPageMenu(page, subMenu);
                                        continue;
                                    }
                                    parentMenu.SetValue(subMenuName, menu, true);
                                }
                                menu.Parent = parentMenu;
                                SetPageMenu(page, menu);
                                parentMenu.Add(menu);
                            }
                        }
                        else
                        {
                            Site.Error($"Invalid menu definition `{menuName}` must be associated with an object (key value pairs).");
                        }
                    }
                }
            }
            else
            {
                Site.Error($"Invalid menu definition `{o}`. Expecting only a string, an object or an array of object/string.");
            }
        }
        
        private MenuObject GetOrCreateMenu(string name)
        {
            var menuObject = Plugin.GetSafeValue<MenuObject>(name);
            if (menuObject == null)
            {
                menuObject = new MenuObject() {Name = name};
                Plugin.SetValue(name, menuObject, false);
            }

            return menuObject;
        }
    }
}