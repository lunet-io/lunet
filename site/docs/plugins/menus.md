---
title: "Menus module"
---

# Menus module

The menus module turns `menu.yml` files into navigation structures.

## Define a site menu

Create a `menu.yml` at the site root:

```yaml
home:
  - {path: readme.md, title: "Home"}
  - {path: docs/readme.md, title: "Docs", folder: true}
```

Menu entries support:
- `path` — a content path to resolve
- `url` — external URL
- `title` — display title (HTML allowed)
- `folder: true` — treat a folder as a collapsible group
- `width` — sidebar width hint (`2..4`, default `3`) used by the default theme layout

## Access in templates

The module exposes menu objects under `site.menu.<name>` and also attaches `menu_item`/`menu()` to pages that belong to a menu.

## Async menu partials (large sidebars)

Large menus can heavily bloat generated HTML because the sidebar is duplicated into every page (especially for generated API docs).

To address this, Lunet can emit a **hashed partial HTML file** for large menus and load it at runtime. The filename includes a content hash, so browsers can cache it aggressively.

Configuration (defaults shown):

```scriban
with menu
  async_load_threshold = 10        # item count threshold; set 0 to disable
  async_partials_folder = "/partials/menus"
end
```

Behavior:

- Below the threshold: `menu.render` inlines the menu HTML (as before).
- At/above the threshold: `menu.render` emits a small placeholder element containing:
  - the partial URL to fetch
  - the list of menu nodes to expand (active chain)
  - the active menu item id(s)
- The actual menu markup is generated once into `async_partials_folder` as `menu-<name>.<hash>.html`.

Notes:

- Active/open state is applied client-side after loading the partial (the partial itself is page-agnostic and cacheable).
- This is transparent to templates: keep using `menu.render`.
- The async loader is injected once via the default JS bundle as `/modules/menus/lunet-menu-async.js` (no per-page inline scripts).

## Using generated API menus

`api.dotnet` generates its own menu hierarchy (`site.menu.api` by default).  
You can reference the generated API root from a manual menu:

```yaml
home:
  - {path: readme.md, title: "Home"}
  - {path: api/readme.md, title: "API Reference", folder: true}
```

With `folder: true`, Lunet automatically reuses the generated API namespace/type/member hierarchy under this item.
