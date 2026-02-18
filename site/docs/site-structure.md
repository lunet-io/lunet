---
title: "Site structure"
---

# Site structure

A Lunet site is a folder containing a `config.scriban` file. Lunet combines your files with a layered meta-folder named `.lunet/` to produce the final output.

## Typical folder layout

```text
mysite/
  config.scriban              ← site configuration (required)
  readme.md                   ← home page (if readme_as_index = true)
  docs/
    getting-started.md        ← content pages
    advanced.md
  css/
    main.scss                 ← SCSS (converted to CSS)
  js/
    main.js                   ← JavaScript
  img/
    logo.svg                  ← static asset
  menu.yml                    ← navigation definition
  .lunet/
    includes/                 ← reusable template fragments
    layouts/                  ← page layout templates
    data/                     ← data files (YAML/JSON/TOML)
    extends/                  ← local themes/extensions
    build/
      www/                    ← generated output (do not edit)
      cache/                  ← download/build cache
```

## The `.lunet/` folder

The `.lunet/` folder contains everything that supports your site but isn’t content itself:

{.table}
| Subfolder | Purpose | Accessed as |
|---|---|---|
| `.lunet/includes/` | Reusable Scriban template fragments | `{{ '{{' }} include "partials/nav.sbn-html" {{ '}}' }}` |
| `.lunet/layouts/` | Page layout templates | Resolved automatically by layout name/type |
| `.lunet/data/` | Data files loaded before content processing | `site.data.<filename>` in templates |
| `.lunet/extends/` | Local themes/extensions | `extend "mytheme"` in config |
| `.lunet/build/` | Generated output and caches | Not accessed directly |

## The layered (virtual) filesystem

This is one of Lunet’s most powerful features. Instead of a single folder, Lunet sees your site through a **layered virtual filesystem** that merges multiple sources:

```text
Priority (highest to lowest):
┌───────────────────────────────────────┐
│  1. Your site files              │  ← highest priority
├───────────────────────────────────────┤
│  2. Theme/extension files        │  ← from extend "..."
├───────────────────────────────────────┤
│  3. Lunet built-in shared files  │  ← shipped with Lunet
└───────────────────────────────────────┘
```

When two layers have a file at the same path, the **higher-priority layer wins**. This applies to everything: layouts, includes, data files, and content.

### Why this matters

The layered filesystem is what makes themes and overrides work seamlessly:

- A theme provides `/.lunet/layouts/_default.sbn-html`.
- You want to customize it? Create `<your-site>/.lunet/layouts/_default.sbn-html` — your file takes priority.
- A theme ships `css/theme.css`? You can override it by placing `css/theme.css` in your site.

You never need to edit theme files. Just create the same path in your site.

### The meta filesystem (`.lunet/`)

The `.lunet/` folder has its own layered resolution:

1. **Build cache** (`<site>/.lunet/build/cache/.lunet/`) — highest priority within `.lunet/`
2. **User `.lunet/`** + **content filesystem layers** (themes) + **shared `.lunet/`** (Lunet built-in)

This means that when a layout or include is requested, Lunet searches through all these layers in priority order.

## Output folder

By default, Lunet writes the generated site to:

```text
<site>/.lunet/build/www/
```

You can override this with the `-o` / `--output-dir` CLI option:

```shell-session
lunet build -o ./public
```

### What goes into the output

- **Pages** (files with front matter) → rendered through layouts, written as HTML (or other format).
- **Static files** (no front matter) → copied as-is.
- **Converted files** (e.g. `.scss`) → converted and written (e.g. as `.css`).

After a build, stale output files (from pages that no longer exist) are automatically deleted.

## Sections

The first directory segment of a content file’s path is its **section**:

- `docs/intro.md` → section = `docs`
- `blog/2024-01-01-hello.md` → section = `blog`
- `readme.md` → section = `""` (root, no section)

Sections matter because:

1. `page.layout` defaults to the section name (so pages in `docs/` look for a `docs` layout first).
2. You can organize layouts by section (`/.lunet/layouts/docs.sbn-html`, `/.lunet/layouts/blog.sbn-html`).
3. RSS feeds and taxonomies can filter by section.
