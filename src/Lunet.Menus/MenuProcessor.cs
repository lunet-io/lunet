// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Lunet.Core;
using Lunet.Yaml;
using Scriban.Runtime;
using Zio;

namespace Lunet.Menus
{
    public class MenuProcessor : ContentProcessor<MenuPlugin>
    {
        public const string MenuFileName = "menu.yml";

        private readonly List<MenuObject> _menus;

        private readonly Dictionary<UPath, ContentObject> _pages;
        
        public MenuProcessor(MenuPlugin plugin) : base(plugin)
        {
            _menus = new List<MenuObject>();
            _pages = new Dictionary<UPath, ContentObject>();
        }

        public override string Name => "menus";

        public override void Process(ProcessingStage stage)
        {
            Debug.Assert(stage == ProcessingStage.BeforeProcessingContent);

            foreach (var menu in _menus)
            {
                if (menu.Path != null)
                {
                    if (!_pages.TryGetValue(menu.Path, out ContentObject page))
                    {
                        Site.Error($"Cannot find menu path {menu.Path}.");
                        continue;
                    }

                    menu.Page = page;
                    SetPageMenu(page, menu);
                    
                    if (page["submenus"] is bool v && v)
                    {
                        var thisDirectory = ((UPath) menu.Path).GetDirectory();
                        
                        foreach (var otherMenu in _menus)
                        {
                            if (otherMenu == menu || otherMenu.Path == null) continue;

                            var menuPath = (UPath) otherMenu.Path;
                            if (menuPath.GetDirectory() == thisDirectory && menuPath.GetName() == MenuFileName)
                            {
                                otherMenu.Path = menu.Path;
                                otherMenu.Page = page;
                                if (menu.Parent != null) 
                                { 
                                    var parentMenu = menu.Parent;
                                    var indexInParent = parentMenu.Children.IndexOf(menu);
                                    if (indexInParent >= 0 && otherMenu != parentMenu)
                                    {
                                        parentMenu.Children[indexInParent] = otherMenu;
                                        otherMenu.Parent = parentMenu;
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // No need to keep the list after the processing
            _menus.Clear();
        }

        public override ContentResult TryProcessContent(ContentObject page, ContentProcessingStage stage)
        {
            Debug.Assert(stage == ContentProcessingStage.AfterLoading);

            _pages.Add(page.Path, page);
            
            if (page.Path.GetName() != MenuFileName)
            {
                return ContentResult.Continue;
            }
            
            // The menu file is not copied to the output!
            page.Discard = true;

            var rawMenu = YamlUtil.FromYaml(page.SourceFile.ReadAllText(), page.SourceFile.FullName);
            DecodeMenu(rawMenu, page);
            
            return ContentResult.Break;
        }

        private void DecodeMenu(object o, ContentObject menuFile, MenuObject parent = null, bool expectingMenuEntry = false)
        {
            if (o is ScriptObject obj)
            {
                if (parent == null)
                {
                    foreach (var keyPair in obj)
                    {
                        var menuName = keyPair.Key;
                        var menuObject = new MenuObject {Path = (string) menuFile.Path, Name = menuName};
                        _menus.Add(menuObject);
                        Plugin.SetValue(menuName, menuObject);
                        DecodeMenu(keyPair.Value, menuFile, menuObject, false);
                    }
                }
                else
                {
                    if (expectingMenuEntry)
                    {
                        var menuObject = new MenuObject {Parent = parent};
                        _menus.Add(menuObject);
                        parent.Children.Add(menuObject);

                        foreach (var keyPair in obj)
                        {
                            var key = keyPair.Key;
                            var value = keyPair.Value;
                            if (key == "path")
                            {
                                var valueAsStr = value?.ToString();

                                if (valueAsStr == null || !UPath.TryParse(valueAsStr, out _))
                                {
                                    throw new LunetException($"The path value `{valueAsStr}` is not a valid path for key `{key}`.");
                                }

                                value = (string) (menuFile.Path.GetDirectory() / (UPath) value?.ToString());
                            }

                            menuObject[key] = value;
                        }
                    }
                    else
                    {
                        foreach (var keyPair in obj)
                        {
                            var key = keyPair.Key;
                            var value = keyPair.Value;
                            if (key == "items")
                            {
                                if (!(value is ScriptArray))
                                {
                                    throw new LunetException($"The items of menu `{parent.Name}` must be an array. The type {value?.GetType()} is not valid for this element.");
                                }
                                DecodeMenu(value, menuFile, parent, true);
                            }
                            else
                            {
                                parent[key] = value;
                            }
                        }
                    }
                }
            }
            else if (o is ScriptArray array)
            {
                if (parent == null)
                {
                    parent = new MenuObject() {Path = (string)menuFile.Path};
                    _menus.Add(parent);
                }
                foreach (var item in array)
                {
                    DecodeMenu(item, menuFile, parent, true);
                }
            }
            else if (o is string str)
            {
                if (!UPath.TryParse(str, out var relPath))
                {
                    throw new LunetException($"Error while parsing menu [{menuFile.Path}]. The string `{str}` is not a valid path.");
                }

                if (parent == null)
                {
                    throw new LunetException($"Error while parsing menu [{menuFile.Path}]. The string `{str}` cannot be a root value.");
                }

                var menuPath = menuFile.Path.GetDirectory() / relPath;

                var menuObject = new MenuObject
                {
                    Path = (string) menuPath,
                    Parent = parent
                };
                _menus.Add(menuObject);

                parent.Children.Add(menuObject);
            }
        }

        private void SetPageMenu(ContentObject page, MenuObject menu)
        {
            page.SetValue("menu", menu);
        }
    }
}