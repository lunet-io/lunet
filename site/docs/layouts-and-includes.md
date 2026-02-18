---
title: "Layouts & includes"
---

# Layouts & includes

Layouts wrap your page content in a reusable template (header, navigation, footer, etc.). Includes are reusable template fragments you can insert from any page or layout.

Both are evaluated at **render time**, not during `config.scriban` loading.

## The two template contexts

Lunet has two distinct contexts where Scriban code runs. Understanding the difference is critical:

{.table}
| Context | When it runs | Main purpose | `include` allowed? | Key objects |
|---|---|---|---|---|
| `config.scriban` | Before content is loaded | Configure site and modules | **No** | Site (as context), module objects |
| Page/layout templates | During content processing | Render HTML/XML output | **Yes** (from `/.lunet/includes/`) | `site`, `page`, `content` |

In **config**, you set up defaults:

```scriban
layout = "_default"
```

In **templates**, you generate output:

```scriban
{{ '{{' }} include "partials/nav.sbn-html" {{ '}}' }}
<main>{{ '{{' }} content {{ '}}' }}</main>
```

## Layouts

### Where layouts live

Layout files are stored under `/.lunet/layouts/` in the site's meta filesystem. This folder can come from:

1. **Your site** — `<site>/.lunet/layouts/`
2. **A theme/extension** — the extension's `.lunet/layouts/`
3. **Lunet built-in shared** — shipped with the Lunet binary

Your local files always take priority (see [Site structure](site-structure.md) for the layered filesystem).

### How layout resolution works

When Lunet renders a page, it looks for a matching layout using three pieces of information:

1. **`page.layout`** — the layout name (defaults to `page.section`, i.e. the first directory segment of the file path)
2. **`page.layout_type`** — the type of rendering (`single` by default)
3. **`page.content_type`** — the output format (`html`, `xml`, `rss`, etc.)

#### Layout name normalization

Layout names cannot contain `\`, `/`, or `.` characters. If present, they are replaced with `-` and a warning is logged. For example, `layout: "my.custom"` in front matter becomes `my-custom`.

If the layout name is empty or null, Lunet falls back to `site.layout` (if set) or `_default`.

#### Step-by-step example

Suppose you have a file `docs/intro.md`:

1. Lunet sets `page.section = "docs"` (first directory segment).
2. `page.layout` defaults to `"docs"` (same as section).
3. `page.layout_type` defaults to `"single"`.
4. Content starts as `markdown`, gets converted to `html`.
5. Lunet searches for a layout matching `(docs, single, html)`.

The search tries these paths under `/.lunet/layouts/`, in order:

```text
docs/single.sbn-html      ← section-specific single layout
docs.single.sbn-html
docs.sbn-html              ← section-specific (any type)
_default/single.sbn-html   ← fallback default single layout
_default.sbn-html          ← fallback default (any type)
```

If `site.layout` is set (e.g. `site.layout = "mybase"`), it is tried between the section-specific and `_default` layouts.

The first matching file wins. All registered extensions for the content type are tried (e.g. `.sbn-html`, `.scriban-html`, `.html`, `.htm`).

### Layout search patterns (full reference)

The search order differs between `single` and all other layout types:

#### Single layout search (8 candidates)

{.table}
| # | Path pattern | Note |
|---|---|---|
| 1 | `{layout}/{type}` | e.g. `docs/single` |
| 2 | `{layout}.{type}` | e.g. `docs.single` |
| 3 | `{layout}` | e.g. `docs` — **single only** (bare name without type) |
| 4 | `{site.layout}/{type}` | only if `site.layout` is set and differs from `layout` |
| 5 | `{site.layout}.{type}` | same condition |
| 6 | `{site.layout}` | **single only** — bare name |
| 7 | `_default/{type}` | only if `layout ≠ _default` |
| 8 | `_default` | **single only** — bare `_default` |

#### List and other types search (6 candidates)

{.table}
| # | Path pattern | Note |
|---|---|---|
| 1 | `{layout}/{type}` | e.g. `docs/list`, `tags/term` |
| 2 | `{layout}.{type}` | e.g. `docs.list`, `tags.term` |
| 3 | `{site.layout}/{type}` | only if `site.layout` is set and differs from `layout` |
| 4 | `{site.layout}.{type}` | same condition |
| 5 | `_default/{type}` | only if `layout ≠ _default` |
| 6 | `_default.{type}` | only if `layout ≠ _default` |

> [!NOTE]
>
> The key difference: `single` tries the bare layout name without the type suffix (paths 3, 6, 8), so `docs.sbn-html` matches single pages in the `docs` section. Non-single types always require the type in the path.

For each candidate path, all registered file extensions for the content type are tried. For `html`, this includes: `.htm`, `.html`, `.scriban-htm`, `.scriban-html`, `.sbn-htm`, `.sbn-html`.

### Common layout file examples

```text
/.lunet/layouts/
  _default.sbn-html          ← catches all single pages with no specific layout
  docs.sbn-html              ← layout for all pages in the docs/ section
  docs.single.sbn-html       ← explicit single layout for docs
  docs.list.sbn-html         ← list layout for docs (used by taxonomy pages, etc.)
  _default.rss.xml           ← RSS feed layout
  tags.term.sbn-html         ← layout for individual tag pages
  tags.terms.sbn-html        ← layout for the tag list page
```

### The `content` variable

Inside a layout template, the `content` variable holds the rendered body of the page (or the output of a previous layout in a chain):

```scriban
<!DOCTYPE html>
<html>
<head><title>{{ '{{' }} page.title {{ '}}' }}</title></head>
<body>
  {{ '{{' }} include "partials/header.sbn-html" {{ '}}' }}
  <main>{{ '{{' }} content {{ '}}' }}</main>
  {{ '{{' }} include "partials/footer.sbn-html" {{ '}}' }}
</body>
</html>
```

### Layout chaining

A layout can specify its own `layout`, `layout_type`, or `layout_content_type` in its front matter. Lunet will then wrap the result in another layout.

At each step, the layout's output becomes the new `content` variable passed to the next layout.

#### Real-world example: three-layer layout chain

A typical theme uses layout chaining to separate concerns:

```text
Page body
  ↓ wrapped by
"default" layout (adds sidebar menu + TOC + content area)
  ↓ wrapped by
"base" layout (adds navbar + footer + Prism setup)
  ↓ wrapped by
"_default" layout (adds <!DOCTYPE>, <html>, <head>, <body>)
```

**`/.lunet/layouts/_default.sbn-html`** — the outermost shell:

```scriban
<!DOCTYPE html>
<html {{ '{{' }} site.html.attributes {{ '}}' }}>
<head>
  {{ '{{' }} include "_builtins/head.sbn-html" {{ '}}' }}
</head>
<body {{ '{{' }} site.html.body.attributes {{ '}}' }}>
{{ '{{' }} content {{ '}}' }}
  {{ '{{' }} include "_builtins/bundle.sbn-html" {{ '}}' }}
</body>
</html>
```

**`/.lunet/layouts/base.sbn-html`** — adds navbar, footer, sets `layout: _default`:

```text
---
layout: _default
---
<div class="container">
  <nav>...</nav>
  <section>{{ '{{' }} content {{ '}}' }}</section>
  <footer>...</footer>
</div>
```

**`/.lunet/layouts/default.sbn-html`** — adds sidebar menu and TOC, sets `layout: base`:

```text
---
layout: base
---
<div class="row">
  <nav>{{ '{{' }} page.menu.render { kind: "menu", collapsible: true } {{ '}}' }}</nav>
  <div>{{ '{{' }} content {{ '}}' }}</div>
  <aside class="js-toc"></aside>
</div>
```

When a page uses `layout: default` (or defaults to it via `site.layout = "default"` in config), the chain runs:

1. Page Markdown → converted to HTML.
2. `default` layout wraps it with sidebar/TOC structure.
3. `base` layout wraps that with navbar/footer.
4. `_default` layout wraps everything with the HTML document shell.

Each step passes its output as the `content` variable to the next layout.

#### Simple variant

A page can bypass the sidebar layout by using `layout: simple`:

```yaml
---
title: Home
layout: simple
---
```

If `simple.sbn-html` sets `layout: base` in its front matter, it skips the sidebar step but still gets navbar/footer and the HTML shell.

> [!WARNING]
>
> Lunet detects infinite loops (same layout/type/content-type tuple visited twice) and stops with an error.

### Markdown → HTML conversion

For Markdown pages, Lunet first **converts** the content from Markdown to HTML, then searches for an HTML layout:

1. Page has `content_type = markdown`.
2. No matching Markdown layout found → Markdown converter runs → content becomes HTML.
3. Layout processor retries with `content_type = html` → finds an HTML layout.

This means your layout files should be `.sbn-html` (not `.sbn-md`) even when wrapping Markdown pages.

### Single vs list rendering

- **`single`** is the default rendering mode for every page (weight 0 — processed first).
- **`list`** is used for index/collection pages (weight 10 — processed after single). In list mode, Lunet injects `pages = site.pages` into the template scope.
- Other list-like types (`rss`, `sitemap`, `term`, `terms`) also have weight 10 and are processed after single pages, but they do **not** inject a `pages` variable — each module injects its own data.

Modules can register custom layout types. Currently registered types:

{.table}
| Type | Weight | Module | Description |
|---|---|---|---|
| `single` | 0 | Built-in | Default for all pages |
| `list` | 10 | Built-in | Index/collection pages |
| `rss` | 10 | [RSS](plugins/rss.md) | RSS feed generation |
| `sitemap` | 10 | [Sitemaps](plugins/sitemaps.md) | Sitemap generation |
| `term` | 10 | [Taxonomies](plugins/taxonomies.md) | Individual term pages (e.g. a specific tag) |
| `terms` | 10 | [Taxonomies](plugins/taxonomies.md) | Term list pages (e.g. all tags) |

### Built-in layouts

Lunet and its modules ship default layouts:

{.table}
| Layout file | Module | Description |
|---|---|---|
| `_default.sbn-html` | Layouts | HTML document shell (`<!DOCTYPE>`, `<html>`, `<head>`, `<body>`) |
| `_default.rss.xml` | [RSS](plugins/rss.md) | Default RSS 2.0 feed |
| `_default.sitemap.xml` | [Sitemaps](plugins/sitemaps.md) | Default sitemap XML |
| `_default.api-dotnet*.sbn-md` | [API .NET](plugins/api-dotnet.md) | API documentation pages |

You can override any built-in layout by creating the same path under your site's `/.lunet/layouts/`.

## Include files

### Where includes live

Include templates live under `/.lunet/includes/` in the meta filesystem. Like layouts, they can come from your site, a theme, or Lunet's built-in shared files.

### Using includes

```scriban
{{ '{{' }} include "partials/nav.sbn-html" {{ '}}' }}
{{ '{{' }} include "_builtins/head.sbn-html" {{ '}}' }}
```

Includes resolve paths relative to `/.lunet/includes/`. You cannot use:
- absolute paths starting with `/` or `\`
- parent traversal (`..`)

Include files are cached during a build — each file is read once and reused across all pages.

> [!TIP]
>
> Includes work in page/layout templates **and** in front matter (both `---` and `+++` blocks). They do **not** work in `config.scriban`.

### Built-in includes

Lunet and its modules ship several built-in includes under `_builtins/`:

{.table}
| Include | Module | Description |
|---|---|---|
| `_builtins/head.sbn-html` | Core | Standard `<head>` content (title, metas, head includes) |
| `_builtins/bundle.sbn-html` | [Bundles](plugins/bundles.md) | CSS/JS bundle injection |
| `_builtins/cards.sbn-html` | [Cards](plugins/cards.md) | OpenGraph/Twitter meta tags |
| `_builtins/google-analytics.sbn-html` | [Tracking](plugins/tracking.md) | Google Analytics snippet |
| `_builtins/livereload.sbn-html` | [Server](plugins/server.md) | Live reload WebSocket script |

Themes typically include these in their base layout. You can override any built-in by creating the same path under your site's `/.lunet/includes/`.

## Template variables reference

### Top-level variables

{.table}
| Variable | Type | Availability | Description |
|---|---|---|---|
| `site` | SiteObject | All templates and front matter | Global site state and module configuration |
| `page` | ContentObject | Page and layout rendering | Current page metadata and content |
| `content` | string | Layout rendering only | The inner content being wrapped |
| `pages` | PageCollection | `list` layouts only | Shortcut to `site.pages` (only for `layout_type = "list"`) |

### `page.*` fields

See [Content & front matter](content-and-frontmatter.md) for the full page variable reference.

### `site.*` fields

{.table}
| Field | Type | Description |
|---|---|---|
| `site.baseurl` | string | Canonical host URL |
| `site.basepath` | string | URL prefix for sub-path hosting |
| `site.environment` | string | Build environment (`dev` or `prod`) |
| `site.layout` | string | Default layout fallback |
| `site.pages` | PageCollection | All loaded pages |
| `site.data` | object | Data loaded from `/.lunet/data/` ([Data modules](plugins/data.md)) |
| `site.html` | object | HTML head/body configuration (see [Configuration](configuration.md)) |
| `site.builtins` | object | Built-in helper functions |

### `site.pages` helpers

{.table}
| Helper | Result |
|---|---|
| `site.pages.by_weight` | Pages ordered by `weight`, then date |
| `site.pages.by_date` | Pages ordered by date |
| `site.pages.by_length` | Pages ordered by source length |
| `site.pages.by_title` | Pages ordered by title |

### `site.builtins.*` (commonly used)

{.table}
| Helper | Description |
|---|---|
| `site.builtins.log.info/warn/error` | Emit log messages from templates |
| `site.builtins.lunet.version` | Current Lunet version string |
| `site.builtins.defer(expr)` | Defer expression evaluation to end of processing |
| `site.builtins.ref(url)` | Resolve an absolute URL using site routing |
| `site.builtins.relref(url)` | Resolve a relative URL from the current page |
| `site.builtins.xref(uid)` | Resolve a UID to an object with `url`, `name`, `fullname`, `page` |

### `site.html.*`

{.table}
| Field | Type | Description |
|---|---|---|
| `site.html.attributes` | string | Attributes injected on `<html>` |
| `site.html.head.title` | string/template | Custom `<title>` override (supports deferred `do/ret`) |
| `site.html.head.metas` | collection | `<meta>` entries rendered in `<head>` |
| `site.html.head.includes` | collection | Include templates rendered in `<head>` |
| `site.html.body.attributes` | string | Attributes injected on `<body>` |
| `site.html.body.includes` | collection | Include templates rendered at end of `<body>` |

## See also

- [Configuration (`config.scriban`)](configuration.md) — `site.html` configuration, `site.layout` fallback
- [Content & front matter](content-and-frontmatter.md) — `page.layout`, `page.layout_type`, `page.content_type`
- [Site structure](site-structure.md) — the layered virtual filesystem for layouts and includes
- [Themes & extensions](themes-and-extends.md) — how extensions provide layouts and includes
- [Modules reference](plugins/readme.md) — per-module layout type documentation
