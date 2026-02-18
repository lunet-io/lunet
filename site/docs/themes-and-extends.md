---
title: "Themes & extensions (extend)"
---

# Themes & extensions (`extend`)

Extensions (themes) are Lunet's way to layer reusable site templates, layouts, and assets on top of your site without modifying your content.

An extension is typically a GitHub repository containing:

- a `dist/` folder with the theme content
- an optional `dist/config.scriban` that runs when the extension is loaded
- an optional `dist/.lunet/` folder for layouts, includes, and data shipped by the theme

## Using extensions

### GitHub repository (latest `main`)

```scriban
extend "owner/repo"
```

Downloads from the default `main` branch. Extensions downloaded without a tag are **re-downloaded once per build session** to pick up latest changes.

### Pinned to a specific tag

```scriban
extend "owner/repo@1.0.0"
```

Tagged extensions are cached locally and not re-downloaded unless the cache is cleaned.

### Full GitHub URL

```scriban
extend "https://github.com/owner/repo@1.0.0"
```

Full URLs work too. A trailing `.git` suffix is automatically stripped.

### Object syntax (advanced)

```scriban
extend {
  repo: "owner/repo",
  tag: "1.0.0",
  directory: "dist",
  public: true
}
```

{.table}
| Property | Aliases | Default | Description |
|---|---|---|---|
| `repo` | `url` | (required) | GitHub `owner/repo` or full URL |
| `tag` | `version` | `null` (latest `main`) | Tag or branch name |
| `directory` | — | `"dist"` | Subfolder within the repository to extract |
| `public` | — | `false` | When `true`, install to `.lunet/extends/` (version-controllable). When `false` (default), install to build cache. |
| `name` | — | (derived from repo) | Display name for the extension |

### The `public` parameter

By default (`public: false`), extensions are installed to the **build cache** at `.lunet/build/cache/.lunet/extends/`. This keeps your site repository clean — cached extensions are not tracked by version control.

When `public: true`, extensions are installed to `.lunet/extends/` within your site directory, so they **are** tracked by version control. This is useful when you want your site to be fully self-contained without network access.

> [!NOTE]
>
> When using the string syntax (`extend "owner/repo"`), extensions are always installed to the build cache (private). Use the object syntax to control installation location.

### Return value

The `extend` function returns an `ExtendObject` (or `null` on failure). You can capture it:

```scriban
myext = extend "owner/repo@1.0.0"
# myext.name, myext.version, myext.url are available
```

## How extensions layer into your site

When you call `extend`, Lunet downloads (and caches) the extension, then adds its files as a **content filesystem layer** below your site:

```text
┌───────────────────────────────────────┐
│  Your site files               │  ← highest priority
├───────────────────────────────────────┤
│  Extension files (dist/)       │  ← from extend "..."
├───────────────────────────────────────┤
│  Lunet built-in shared files   │  ← lowest priority
└───────────────────────────────────────┘
```

Everything under `dist/` in the extension becomes available as if it were part of your site, but at a **lower priority**. This means your files always win when both layers have a file at the same path.

### What the extension provides

The extension's `dist/` folder typically includes:

```text
dist/
  config.scriban            ← extension configuration (runs during your config)
  readme.md                 ← optional default home page
  css/
    theme.css               ← theme styles
  .lunet/
    layouts/
      _default.sbn-html     ← default layout
      docs.sbn-html         ← section-specific layout
    includes/
      partials/
        header.sbn-html     ← reusable template fragments
        footer.sbn-html
    data/
      theme.yml             ← theme data
```

### Config execution order

When your `config.scriban` calls `extend "owner/repo"`:

1. Lunet downloads/caches the extension.
2. The extension's `config.scriban` (at the root of the extracted `dist/` folder) is **imported immediately** — it runs in the current site context with full site function access.
3. The extension's files are added as a filesystem layer.
4. Execution returns to your `config.scriban` and continues with the next line.

This means:

- The extension's config can set defaults (e.g. `layout = "base"`).
- Your config can override those defaults after the `extend` call.
- If the extension registers bundles, you can add to them afterward.

## Overriding extension files

Your local files always take priority. Common override patterns:

### Override a layout

The extension provides `dist/.lunet/layouts/_default.sbn-html`. To customize it, create the same path in your site:

```text
<your-site>/.lunet/layouts/_default.sbn-html
```

Your version will be used instead of the extension's.

### Override an include

The extension provides `dist/.lunet/includes/partials/header.sbn-html`. Override it:

```text
<your-site>/.lunet/includes/partials/header.sbn-html
```

### Override content

The extension provides `dist/css/theme.css`. Override it:

```text
<your-site>/css/theme.css
```

> [!TIP]
>
> You never need to modify extension files. Just create the same path in your site. This makes theme updates safe — your overrides are preserved when you update the extension tag.

## Local theme development

To iterate on a theme without publishing to GitHub, put it under your site's `.lunet/extends/` folder:

```text
<your-site>/.lunet/extends/mytheme/
  config.scriban
  .lunet/
    layouts/
      _default.sbn-html
```

Then load it by name:

```scriban
extend "mytheme"
```

When the name doesn't contain a `/`, Lunet looks for it in `/.lunet/extends/<name>/` (checking both the cache and site directories).

## Multiple extensions

You can call `extend` multiple times. Each extension adds a new filesystem layer. Later extensions have **higher** priority than earlier ones (but all are below your site files):

```scriban
extend "base-theme/base@1.0.0"     # lowest extension priority
extend "custom-theme/custom@2.0.0" # higher priority than base
```

If you call `extend` with the same extension (same name/tag/directory) twice, the duplicate is silently reused without re-downloading.

## Accessing loaded extensions in templates

The `extends` builtin (note the plural `s`) provides a read-only list of all loaded extensions:

```scriban
{{ '{{' }} for ext in extends {{ '}}' }}
  {{ '{{' }} ext.name {{ '}}' }} - {{ '{{' }} ext.version {{ '}}' }}
{{ '{{' }} end {{ '}}' }}
```

Each `ExtendObject` exposes:

{.table}
| Property | Type | Description |
|---|---|---|
| `name` | string | Extension display name |
| `version` | string | Tag/version (null if using latest main) |
| `url` | string | GitHub URL (null for local extends) |

## Cache paths

{.table}
| Scenario | Cache location |
|---|---|
| Private (default) | `.lunet/build/cache/.lunet/extends/github/<owner>/<repo>/<tag>/<directory>/` |
| Public | `.lunet/extends/github/<owner>/<repo>/<tag>/<directory>/` |
| Local | `.lunet/extends/<name>/` |

Run `lunet clean` to clear all cached extensions and force a re-download.

## See also

- [Extends module](plugins/extends.md) — detailed module reference with all query forms
- [Site structure](site-structure.md) — the layered virtual filesystem
- [Configuration (`config.scriban`)](configuration.md) — how extension config integrates with site config
- [Layouts & includes](layouts-and-includes.md) — how extension layouts/includes are resolved
