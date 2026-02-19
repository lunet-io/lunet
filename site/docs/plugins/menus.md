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

All `path` values are resolved relative to the directory containing the `menu.yml` file.

### Menu entry properties

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `path` | string | — | Path to a content file (resolved relative to the `menu.yml` directory) |
| `url` | string | — | External URL (use instead of `path` for external links) |
| `title` | string | — | Display title (HTML is allowed, e.g. `<i class='bi bi-house'></i> Home`) |
| `pre` | string | — | HTML prepended before the title text |
| `post` | string | — | HTML appended after the title text |
| `folder` | bool | `false` | Adopt generated child menus from the same directory |
| `self` | bool | `false` | Mark this entry as the breadcrumb root (useful for a "Home" link) |
| `separator` | bool | `false` | Render as a visual separator instead of a link |
| `target` | string | — | HTML `target` attribute on the link (e.g. `"_blank"`) |
| `env` | string/array | — | Only include this item for specific environments (for example `env: dev` or `env: [dev, preview]`) |
| `width` | int | `3` | Sidebar width hint (clamped to `2`–`4`), used by themes |
| `link_class` | string | — | CSS classes added to the `<a>` element |
| `link_class_active` | string | — | CSS classes added when the item is active |
| `list_item_class` | string | — | CSS classes added to the `<li>` element |

`env` matching is case-insensitive. You can also exclude an environment with `!` (for example `env: "!prod"`).

### String shorthand

A bare string in a menu array is treated as a path reference:

```yaml
doc:
  - readme.md
  - getting-started.md
```

This is equivalent to `{path: readme.md}`. The title is taken from the page's front matter.

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

### `page.menu_item`

Points to the specific menu entry corresponding to the current page. This is used internally for active-state detection and breadcrumb rendering.

### `menu.render` options

The `render` function accepts an options object:

{.table}
| Option | Type | Default | Description |
|---|---|---|---|
| `kind` | string | `"menu"` | Rendering style: `"nav"` for horizontal navbar, `"menu"` for vertical sidebar, `"breadcrumb"` for breadcrumbs |
| `depth` | int | max | Maximum nesting depth to render |
| `collapsible` | bool | `false` | When `true`, submenus can be collapsed/expanded (Bootstrap collapse) |
| `async` | bool | `true` | Allow async partial loading for large menus (only for `kind: "menu"`) |
| `list_class` | string | — | CSS class added to the `<ol>` element |
| `list_item_class` | string | — | CSS class added to the `<li>` element |
| `link_class` | string | — | CSS class added to the `<a>` element |
| `link_class_active` | string | — | CSS class added to the active `<a>` element |
| `link_args` | string | — | Extra HTML attributes on `<a>` elements |
| `link_args_active` | string | — | Extra HTML attributes on active `<a>` elements |

### `menu.breadcrumb`

Render a breadcrumb trail for the current page:

```scriban
{{ '{{' }} if page.menu != null {{ '}}' }}
  {{ '{{' }} page.menu.breadcrumb {{ '}}' }}
{{ '{{' }} end {{ '}}' }}
```

The `self: true` entry in the menu definition becomes the breadcrumb root.

### Active state detection

A menu item is marked as active when:

1. The current page's `menu_item` matches the menu item or one of its ancestors (so parent items are highlighted when a child is active).
2. If the page has no explicit `menu_item`, a fallback checks whether the page shares the same section as the menu item's page.

## Async menu partials (large sidebars)

Large menus can heavily bloat generated HTML because the sidebar markup is duplicated into every page (especially for generated API docs with hundreds of pages).

To address this, Lunet can emit a **hashed partial HTML file** for large menus and load it at runtime via JavaScript. The filename includes a content hash, so browsers can cache it aggressively.

Configuration in `config.scriban` (defaults shown):

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
- Async loading is only used for `kind: "menu"` (sidebars). Navbars (`kind: "nav"`) always render inline.
- You can disable async for a specific render call with `{ async: false }`.

When async loading is enabled, the menu plugin automatically injects a JavaScript file (`lunet-menu-async.js`) into the default bundle.

## Using generated API menus

The [API (.NET) module](api-dotnet.md) generates its own menu hierarchy (`site.menu.api` by default). You can reference it from a manual menu:

```yaml
home:
  - {path: readme.md, title: "Home"}
  - {path: api/readme.md, title: "API Reference", folder: true, env: dev}
```

With `folder: true`, Lunet automatically adopts the generated API namespace/type/member hierarchy under this item. This avoids maintaining a separate `api/menu.yml`.

## See also

- [API (.NET) module](api-dotnet.md) — generates menus for API documentation
- [Layouts & includes](../layouts-and-includes.md) — using menus in layout templates
