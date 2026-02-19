// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lunet.Bundles;
using Lunet.Core;
using Lunet.Yaml;
using Scriban.Runtime;
using Zio;

namespace Lunet.Menus;

public class MenuProcessor : ContentProcessor<MenuPlugin>
{
    public const string MenuFileName = "menu.yml";
    private const string MenuItemSelfKey = "self";

    private readonly List<MenuObject> _menus;
    private bool _asyncLoaderInjected;

    public MenuProcessor(MenuPlugin plugin) : base(plugin)
    {
        _menus = new List<MenuObject>();
    }

    public override string Name => "menus";

    public override void Process(ProcessingStage stage)
    {
        Debug.Assert(stage == ProcessingStage.BeforeProcessingContent);

        if (!_asyncLoaderInjected && Plugin.AsyncLoadThreshold > 0)
        {
            var defaultBundle = Plugin.BundlePlugin.GetOrCreateBundle(null);
            defaultBundle.InsertLink(0, BundleObjectProperties.JsType, "/modules/menus/lunet-menu-async.js");
            _asyncLoaderInjected = true;
        }

        FilterMenusForCurrentEnvironment();

        foreach (var menu in _menus)
        {
            if (menu.Path != null)
            {
                if (!Site.Content.Finder.TryFindByPath(menu.Path, out ContentObject? page) || page is null)
                {
                    Site.Error($"Cannot find menu path {menu.Path}.");
                    continue;
                }

                menu.Page = page;
                TryAdoptGeneratedChildren(menu, page.GetSafeValue<MenuObject>("menu_item"));
                SetPageMenu(page, menu, false);

                if (!menu.Folder) continue;

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
                                otherMenu.Title = menu.Title;
                                otherMenu.Pre = menu.Pre;
                                otherMenu.Post = menu.Post;
                                otherMenu.Separator = menu.Separator;
                                if (!otherMenu.ContainsKey("width") && menu.ContainsKey("width"))
                                {
                                    otherMenu.Width = menu.Width;
                                }
                                SetPageMenu(page, otherMenu, true);
                            }
                        }

                        break;
                    }
                }
            }
        }

        ApplyRootSelfItems();

        // No need to keep the list after the processing
        _menus.Clear();
    }

    private void FilterMenusForCurrentEnvironment()
    {
        var visibleMenus = new HashSet<MenuObject>();
        foreach (var root in _menus.Where(x => x.Parent is null))
        {
            FilterMenusForCurrentEnvironment(root, visibleMenus);
        }

        _menus.RemoveAll(menu => !visibleMenus.Contains(menu));
    }

    private bool FilterMenusForCurrentEnvironment(MenuObject menu, HashSet<MenuObject> visibleMenus)
    {
        if (!IsVisibleForCurrentEnvironment(menu["env"]))
        {
            return false;
        }

        visibleMenus.Add(menu);
        for (var index = menu.Children.Count - 1; index >= 0; index--)
        {
            var child = menu.Children[index];
            if (FilterMenusForCurrentEnvironment(child, visibleMenus))
            {
                continue;
            }

            menu.Children.RemoveAt(index);
        }

        return true;
    }

    private void ApplyRootSelfItems()
    {
        foreach (var root in _menus)
        {
            if (root.Parent is not null || root.Children.Count == 0)
            {
                continue;
            }

            // Only patch roots that still point to a menu.yml page (i.e. not merged into a folder entry).
            if (root.Page is not null && root.Page.Path.GetName() != MenuFileName)
            {
                continue;
            }

            var selfItem = root.Children.FirstOrDefault(x => x.GetSafeValue<bool>(MenuItemSelfKey));
            if (selfItem is null)
            {
                continue;
            }

            if (selfItem.Page is null)
            {
                continue;
            }

            // Make the root behave as an alias of the self item for breadcrumb/title purposes,
            // while keeping menu item activation on the actual page menu item.
            root.Title = selfItem.Title;
            root.Pre = selfItem.Pre;
            root.Post = selfItem.Post;
            root.Url = selfItem.Url;
            root.Target = selfItem.Target;
            root.Page = selfItem.Page;
        }
    }

    public override ContentResult TryProcessContent(ContentObject page, ContentProcessingStage stage)
    {
        Debug.Assert(stage == ContentProcessingStage.Running);

        lock (_menus)
        {
            if (page.Path.GetName() != MenuFileName)
            {
                return ContentResult.Continue;
            }
                
            // The menu file is not copied to the output!
            page.Discard = true;

            var rawMenu = YamlUtil.FromText(page.SourceFile.ReadAllText(), page.SourceFile.FullName);
            // Lock the menus as we can work concurrently
            DecodeMenu(rawMenu, page);
        }

        return ContentResult.Break;
    }

    private void DecodeMenu(object? o, ContentObject menuFile, MenuObject? parent = null, bool expectingMenuEntry = false)
    {
        if (o is ScriptObject obj)
        {
            if (parent == null)
            {
                foreach (var keyPair in obj)
                {
                    var menuName = keyPair.Key;
                    var menuObject = new MenuObject {Path = (string) menuFile.Path, Name = menuName, Title = GetDefaultRootTitle(menuName)};
                    _menus.Add(menuObject);
                    Plugin.SetValue(menuName, menuObject);
                    DecodeMenu(keyPair.Value, menuFile, menuObject, false);
                }
            }
            else
            {
                if (expectingMenuEntry)
                {
                    if (obj.TryGetValue("env", out var envValue) && !IsVisibleForCurrentEnvironment(envValue))
                    {
                        return;
                    }

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

                            value = (string) (menuFile.Path.GetDirectory() / (UPath) valueAsStr);
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

    public void SetPageMenu(ContentObject page, MenuObject menu, bool force)
    {
        var shouldSet = force || !page.ContainsKey("menu_item");
        if (!shouldSet && page.GetSafeValue<MenuObject>("menu_item") is MenuObject existingMenu)
        {
            shouldSet = existingMenu.Generated && !menu.Generated && (menu.Folder || menu.Children.Count > 0);
        }

        // If a menu is already set, we keep it as the first one is the most relevant one
        if (!shouldSet)
        {
            return;
        }

        page.SetValue("menu_item", menu);

        Func<MenuObject?> resolveMenu = () =>
        {
            var parentMenu = menu;
            while (parentMenu is not null)
            {
                var shouldClimb = parentMenu.Children.Count == 0;
                if (!shouldClimb && parentMenu.Generated && parentMenu.Parent is { Generated: true })
                {
                    // Keep climbing in generated trees (e.g API menus) so we expose
                    // a stable parent menu block instead of a leaf-local subtree.
                    shouldClimb = true;
                }

                if (!shouldClimb && parentMenu.Generated && parentMenu.Parent is { Generated: false, Folder: true })
                {
                    // When generated trees are grafted under a manual folder menu,
                    // keep climbing one more level so the sidebar starts from the
                    // manual folder item (same visual behavior as menu.yml folders).
                    shouldClimb = true;
                }

                if (!shouldClimb)
                {
                    break;
                }

                parentMenu = parentMenu.Parent;
            }

            return parentMenu;
        };

        page.SetValue("menu", DelegateCustomFunction.CreateFunc(resolveMenu));
    }

    private static string GetDefaultRootTitle(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Home";
        }

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Home";
        }

        // Basic title casing: keep numbers, capitalize first character.
        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }

    private bool IsVisibleForCurrentEnvironment(object? envValue)
    {
        var tokens = EnumerateEnvironmentTokens(envValue).ToList();
        if (tokens.Count == 0)
        {
            return true;
        }

        var currentEnvironment = Site.Environment;
        if (string.IsNullOrWhiteSpace(currentEnvironment))
        {
            currentEnvironment = "prod";
        }

        bool hasIncludedEnvironments = false;
        bool isIncludedEnvironment = false;

        foreach (var token in tokens)
        {
            if (token.Length == 0)
            {
                continue;
            }

            if (token[0] == '!')
            {
                var excludedEnvironment = token[1..].Trim();
                if (excludedEnvironment.Length > 0 &&
                    currentEnvironment.Equals(excludedEnvironment, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                continue;
            }

            hasIncludedEnvironments = true;
            if (currentEnvironment.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                isIncludedEnvironment = true;
            }
        }

        return !hasIncludedEnvironments || isIncludedEnvironment;
    }

    private static IEnumerable<string> EnumerateEnvironmentTokens(object? envValue)
    {
        switch (envValue)
        {
            case null:
                yield break;
            case string environment:
                foreach (var token in SplitEnvironmentTokens(environment))
                {
                    yield return token;
                }

                yield break;
            case ScriptArray array:
                foreach (var item in array)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    foreach (var token in SplitEnvironmentTokens(item.ToString() ?? string.Empty))
                    {
                        yield return token;
                    }
                }

                yield break;
            default:
                foreach (var token in SplitEnvironmentTokens(envValue.ToString() ?? string.Empty))
                {
                    yield return token;
                }

                yield break;
        }
    }

    private static IEnumerable<string> SplitEnvironmentTokens(string value)
    {
        return value.Split([',', ';', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static void TryAdoptGeneratedChildren(MenuObject menu, MenuObject? existingMenu)
    {
        if (!menu.Folder || menu.Children.Count > 0 || existingMenu is null || !existingMenu.Generated || existingMenu.Children.Count == 0)
        {
            return;
        }

        foreach (var child in existingMenu.Children)
        {
            child.Parent = menu;
            menu.Children.Add(child);
        }

        if (!menu.ContainsKey("width"))
        {
            menu.Width = existingMenu.Width;
        }
    }
}
