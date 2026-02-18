---
title: "Site structure"
---

# Site structure

A Lunet site is a folder containing a `config.scriban` file. Lunet combines your files with a layered meta-folder named `.lunet/` to produce the final output.

## Typical folder layout

```text
mysite/
  config.scriban              ← site configuration (required)
  readme.md                   ← home page (if readme_as_index = true, the default)
  docs/
    getting-started.md        ← content pages
    advanced.md
  css/
    main.scss                 ← SCSS (converted to CSS by Dart Sass)
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
    modules/                  ← module assets (search db, menu JS, etc.)
    build/
      www/                    ← generated output (do not edit)
      cache/                  ← download/build cache
```

## The `.lunet/` folder

The `.lunet/` folder contains everything that supports your site but isn't content itself:

{.table}
| Subfolder | Purpose | Accessed as |
|---|---|---|
| `.lunet/includes/` | Reusable Scriban template fragments | `{{ '{{' }} include "partials/nav.sbn-html" {{ '}}' }}` |
| `.lunet/layouts/` | Page layout templates | Resolved automatically by layout name/type (see [Layouts & includes](layouts-and-includes.md)) |
| `.lunet/data/` | Data files loaded before content processing | `site.data.<filename>` in templates (see [Data modules](plugins/data.md)) |
| `.lunet/extends/` | Local themes/extensions | `extend "mytheme"` in config (see [Themes & extensions](themes-and-extends.md)) |
| `.lunet/modules/` | Module runtime assets | Search database, menu JS, API doc assets — managed by plugins |
| `.lunet/build/` | Generated output and caches | Not accessed directly |

### Inside `.lunet/build/`

{.table}
| Path | Purpose |
|---|---|
| `.lunet/build/www/` | Default output directory. Contains the generated site ready to deploy. |
| `.lunet/build/cache/` | Download and build cache. Extensions, npm resources, and API extractor output are cached here. |
| `.lunet/build/cache/.lunet/` | Cached meta files (layouts/includes from extensions). |

## The layered (virtual) filesystem

This is one of Lunet's most powerful features. Instead of a single folder, Lunet sees your site through a **layered virtual filesystem** that merges multiple sources:

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

The `.lunet/` folder has its own layered resolution, composed from two sources:

1. **Aggregated `.lunet/`** — the `.lunet/` subtree from the main layered filesystem (your site > themes > shared) — **highest priority**
2. **Cache `.lunet/`** (`.lunet/build/cache/.lunet/`) — cached meta files — **lowest priority**

Your site's `.lunet/` files always take precedence over cached copies. When a layout or include is requested, Lunet searches through all these layers in priority order.

### Built-in shared files

Lunet ships a set of default layouts, includes, and module assets under a `shared/` folder alongside the Lunet binary. These form the lowest-priority layer. Key files include:

{.table}
| Path | Module | Description |
|---|---|---|
| `/.lunet/layouts/_default.sbn-html` | Layouts | Minimal HTML document shell |
| `/.lunet/layouts/_default.rss.xml` | [RSS](plugins/rss.md) | Default RSS 2.0 feed layout |
| `/.lunet/layouts/_default.sitemap.xml` | [Sitemaps](plugins/sitemaps.md) | Default sitemap layout |
| `/.lunet/includes/_builtins/head.sbn-html` | Core | Default `<head>` content |
| `/.lunet/includes/_builtins/bundle.sbn-html` | [Bundles](plugins/bundles.md) | CSS/JS bundle injection |
| `/.lunet/includes/_builtins/cards.sbn-html` | [Cards](plugins/cards.md) | OpenGraph/Twitter meta tags |
| `/.lunet/includes/_builtins/google-analytics.sbn-html` | [Tracking](plugins/tracking.md) | Analytics snippet |
| `/.lunet/includes/_builtins/livereload.sbn-html` | [Server](plugins/server.md) | Live reload WebSocket script |
| `/.lunet/modules/search/sqlite/*` | [Search](plugins/search.md) | SQL.js WASM, search client JS |
| `/.lunet/modules/menus/*` | [Menus](plugins/menus.md) | Async menu JS |

## File inclusion and exclusion

Not all files in your site folder are processed. Lunet uses three glob collections to decide:

1. **`force_excludes`** — always excluded, cannot be overridden: `**/.lunet/build/**`, `/config.scriban`
2. **`includes`** — overrides `excludes`: `**/.lunet/**`
3. **`excludes`** — skipped unless matched by `includes`: `**/~*/**`, `**/.*/**`, `**/_*/**`

Files not matching any rule are included. See [Configuration](configuration.md) for customizing these patterns.

## Output folder

By default, Lunet writes the generated site to:

```text
<site>/.lunet/build/www/
```

You can override this with the `-o` / `--output-dir` CLI option:

```shell-session
lunet build -o ./public
```

When `-o` is specified, the path is resolved relative to the current working directory.

### What goes into the output

- **Pages** (files with front matter) → rendered through layouts, written as HTML (or other format).
- **Static files** (no front matter) → copied as-is.
- **Converted files** (e.g. `.scss`) → converted and written (e.g. as `.css`).

After a build, stale output files (from pages that no longer exist in the source) are automatically deleted. Lunet also detects and reports duplicate output paths — if two source files would produce the same output file, an error is logged.

### Output path rules

For HTML-like content with folder URLs (the default):

- `docs/intro.md` → output: `.lunet/build/www/docs/intro/index.html`
- `about.md` → output: `.lunet/build/www/about/index.html`
- `readme.md` → output: `.lunet/build/www/index.html` (when `readme_as_index = true`)

With `url_as_file = true`:

- `docs/intro.md` → output: `.lunet/build/www/docs/intro.html`

## Sections

The first directory segment of a content file's path is its **section**:

- `docs/intro.md` → section = `docs`
- `blog/2024-01-01-hello.md` → section = `blog`
- `readme.md` → section = `""` (root, no section)

Sections matter because:

1. `page.layout` defaults to the section name (so pages in `docs/` look for a `docs` layout first). See [Layouts & includes](layouts-and-includes.md).
2. You can organize layouts by section (`/.lunet/layouts/docs.sbn-html`, `/.lunet/layouts/blog.sbn-html`).
3. [RSS feeds](plugins/rss.md) and [taxonomies](plugins/taxonomies.md) can filter by section.

The section is available as `page.section` in templates, and `page.path_in_section` gives the remaining path after the section directory (e.g. `/intro.md` within section `docs`).

## The `lunet init` skeleton

When you run `lunet init mysite`, Lunet copies a minimal skeleton from its built-in shared files:

- `config.scriban` — a template config with title, baseurl, and basic bundle/scss setup
- `readme.md` — a sample home page with front matter

The skeleton provides just enough to build and serve a working site. See [Getting started](getting-started.md) for a walkthrough.

## See also

- [Configuration (`config.scriban`)](configuration.md) — site variables, includes/excludes patterns
- [Content & front matter](content-and-frontmatter.md) — pages vs static files, URL rules
- [Layouts & includes](layouts-and-includes.md) — layout resolution in `/.lunet/layouts/`
- [Themes & extensions](themes-and-extends.md) — how extensions layer into the virtual filesystem
- [CLI reference](cli.md) — `lunet init`, `lunet clean`, output directory options
