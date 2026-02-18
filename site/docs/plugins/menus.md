---
title: "Menus module"
---

# Menus module

The menus module turns `menu.yml` files into navigation structures that your layouts can render as navbars, sidebars, and breadcrumbs.

## Defining menus

### Site-level menu (`menu.yml`)

Create a `menu.yml` at your site root to define the top-level navigation:

```yaml
home:
  - {path: readme.md, title: "Home", self: true}
  - {path: docs/readme.md, title: "Docs", folder: true}

home2:
  - {url: "https://github.com/org/repo", title: "GitHub", link_class: btn btn-info}
```

Each top-level key (e.g. `home`, `home2`) creates a separate menu accessible as `site.menu.home`, `site.menu.home2`, etc.

> [!TIP]
>
> A common theme pattern uses `home` for the left navbar and `home2` for the right navbar (e.g. external links styled as buttons).

### Section menus (subdirectory `menu.yml`)

You can also place `menu.yml` in subdirectories to define section-specific sidebar menus:

```yaml
# docs/menu.yml
doc:
  - {path: readme.md, title: "User Guide"}
  - {path: getting-started.md, title: "Getting Started"}
  - {path: configuration.md, title: "Configuration"}
```

Pages inside `docs/` will automatically get `page.menu` set to this menu, which layouts can render as a sidebar.

### Menu entry properties

{.table}
| Property | Type | Description |
|---|---|---|
| `path` | string | Path to a content file (resolved to its URL) |
| `url` | string | External URL (use instead of `path` for external links) |
| `title` | string | Display title (HTML is allowed, e.g. `<i class='bi bi-house'></i> Home`) |
| `folder` | bool | When `true`, child pages from the folder are included automatically |
| `self` | bool | Mark this entry as the breadcrumb root (useful for a "Home" link) |
| `width` | int | Sidebar width hint (`2`–4`, default `3`) used by themes |
| `link_class` | string | CSS classes added to the `<a>` element (e.g. `btn btn-info`) |

## Using menus in templates

### `site.menu.<name>`

Every menu defined in `menu.yml` is available on `site.menu`:

```scriban
{{ '{{' }} site.menu.home.render { depth: 1, kind: "nav" } {{ '}}' }}
```

### `page.menu`

When a page belongs to a section menu, `page.menu` is set automatically. Use it to render a sidebar:

```scriban
{{ '{{' }} if page.menu {{ '}}' }}
  {{ '{{' }} page.menu.render { kind: "menu", collapsible: true, depth: 2 } {{ '}}' }}
{{ '{{' }} end {{ '}}' }}
```

### `menu.render` options

The `render` function accepts an options object:

{.table}
| Option | Type | Default | Description |
|---|---|---|---|
| `kind` | string | `"menu"` | Rendering style: `"nav"` for horizontal navbar, `"menu"` for vertical sidebar |
| `depth` | int | `2` | Maximum nesting depth to render |
| `collapsible` | bool | `false` | When `true`, submenus can be collapsed/expanded |
| `list_class` | string | — | CSS class added to the `<ul>` element |

### `menu.breadcrumb`

Render a breadcrumb trail for the current page:

```scriban
{{ '{{' }} if page.menu != null {{ '}}' }}
  {{ '{{' }} page.menu.breadcrumb {{ '}}' }}
{{ '{{' }} end {{ '}}' }}
```

The `self: true` entry in the menu definition becomes the breadcrumb root.

## Async menu partials (large sidebars)

Large menus can heavily bloat generated HTML because the sidebar markup is duplicated into every page (especially for generated API docs with hundreds of pages).

To address this, Lunet can emit a **hashed partial HTML file** for large menus and load it at runtime via JavaScript. The filename includes a content hash, so browsers can cache it aggressively.

Configuration (defaults shown):

```scriban
with menu
  async_load_threshold = 10        # item count threshold; set 0 to disable
  async_partials_folder = "/partials/menus"
end
```

Behavior:

- Below the threshold: `menu.render` inlines the menu HTML directly into each page.
- At/above the threshold: `menu.render` emits a small placeholder element. The actual menu markup is generated once into `async_partials_folder` as `menu-<name>.<hash>.html` and loaded client-side.
- Active/open state is applied client-side after loading (the partial is page-agnostic and cacheable).
- This is transparent to templates: keep using `menu.render` regardless of menu size.

## Using generated API menus

The [API (.NET) module](api-dotnet.md) generates its own menu hierarchy (`site.menu.api` by default). You can reference it from a manual menu:

```yaml
home:
  - {path: readme.md, title: "Home"}
  - {path: api/readme.md, title: "API Reference", folder: true}
```

With `folder: true`, Lunet automatically reuses the generated API namespace/type/member hierarchy under this item. This avoids maintaining a separate `api/menu.yml`.
