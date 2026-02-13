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

## Access in templates

The module exposes menu objects under `site.menu.<name>` and also attaches `menu_item`/`menu()` to pages that belong to a menu.

