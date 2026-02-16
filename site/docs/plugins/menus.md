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

## Using generated API menus

`api.dotnet` generates its own menu hierarchy (`site.menu.api` by default).  
You can reference the generated API root from a manual menu:

```yaml
home:
  - {path: readme.md, title: "Home"}
  - {path: api/readme.md, title: "API Reference", folder: true}
```

With `folder: true`, Lunet automatically reuses the generated API namespace/type/member hierarchy under this item.
