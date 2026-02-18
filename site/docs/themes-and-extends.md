---
title: "Themes & extensions (extend)"
---

# Themes & extensions (`extend`)

Extensions (themes) are Lunet’s way to layer reusable site templates, layouts, and assets on top of your site without modifying your content.

An extension is typically a GitHub repository containing:

- a `dist/` folder with the theme content
- an optional `dist/config.scriban` that runs when the extension is loaded
- an optional `dist/.lunet/` folder for layouts, includes, and data shipped by the theme

## Using extensions

### GitHub repository (latest `main`)

```scriban
extend "owner/repo"
```

### Pinned to a specific tag

```scriban
extend "owner/repo@1.0.0"
```

### Object syntax (advanced)

```scriban
extend {
  repo: "owner/repo",
  tag: "1.0.0",
  directory: "dist",
  public: true
}
```

- `directory` defaults to `dist`.
- If `dist/config.scriban` exists in the extension, it is imported automatically.

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

The extension’s `dist/` folder typically includes:

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
2. The extension’s `dist/` is added as a filesystem layer.
3. If `dist/config.scriban` exists, it is **imported immediately** (runs in the current site context).
4. Execution returns to your `config.scriban` and continues with the next line.

This means:

- The extension’s config can set defaults (e.g. `layout = "base"`).
- Your config can override those defaults after the `extend` call.
- If the extension registers bundles, you can add to them afterward.

## Overriding extension files

Your local files always take priority. Common override patterns:

### Override a layout

The extension provides `dist/.lunet/layouts/_default.sbn-html`. To customize it, create the same path in your site:

```text
<your-site>/.lunet/layouts/_default.sbn-html
```

Your version will be used instead of the extension’s.

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

To iterate on a theme without publishing to GitHub, put it under your site’s `.lunet/extends/` folder:

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

When the name doesn’t contain a `/`, Lunet looks for it in `/.lunet/extends/<name>/` instead of GitHub.

## Multiple extensions

You can call `extend` multiple times. Each extension adds a new filesystem layer. Later extensions have **higher** priority than earlier ones (but all are below your site files):

```scriban
extend "base-theme/base@1.0.0"     # lowest extension priority
extend "custom-theme/custom@2.0.0" # higher priority than base
```
