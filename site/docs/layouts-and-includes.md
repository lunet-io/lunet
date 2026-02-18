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

Layout files are stored under `/.lunet/layouts/` in the site’s meta filesystem. This folder can come from:

1. **Your site** — `<site>/.lunet/layouts/`
2. **A theme/extension** — the extension’s `.lunet/layouts/`
3. **Lunet built-in shared** — shipped with the Lunet binary

Your local files always take priority (see [Site structure](site-structure.md) for the layered filesystem).

### How layout resolution works

When Lunet renders a page, it looks for a matching layout using three pieces of information:

1. **`page.layout`** — the layout name (defaults to `page.section`, i.e. the first directory segment of the file path)
2. **`page.layout_type`** — the type of rendering (`single` by default)
3. **`page.content_type`** — the output format (`html`, `xml`, `rss`, etc.)

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

The first matching file wins. All registered extensions for the content type are tried (e.g. `.sbn-html`, `.scriban-html`, `.html`).

### Layout search patterns (full reference)

{.table}
| `layout_type` | Paths tried (in order) under `/.lunet/layouts/` |
|---|---|
| `single` | `<layout>/<type>`, `<layout>.<type>`, `<layout>`, `<site.layout>/<type>`, `<site.layout>.<type>`, `<site.layout>`, `_default/<type>`, `_default` |
| `list` and other types | `<layout>/<type>`, `<layout>.<type>`, `<site.layout>/<type>`, `<site.layout>.<type>`, `_default/<type>`, `_default.<type>` |

Where `<layout>` = `page.layout`, `<type>` = `page.layout_type`.

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

A layout can specify its own `layout`, `layout_type`, or `layout_content_type` in its front matter. Lunet will then wrap the result in another layout:

```text
+++
layout = "base"
layout_type = "single"
+++
<article>{{ '{{' }} content {{ '}}' }}</article>
```

This creates a chain: page body → first layout → second layout (`base`). Lunet detects infinite loops (same layout tuple visited twice) and stops with an error.

### Markdown → HTML conversion

For Markdown pages, Lunet first **converts** the content from Markdown to HTML, then searches for an HTML layout:

1. Page has `content_type = markdown`.
2. Markdown converter runs → content becomes HTML.
3. Layout processor searches for an HTML layout.

This means your layout files should be `.sbn-html` (not `.sbn-md`) even when wrapping Markdown pages.

### Single vs list rendering

- **`single`** is the default rendering mode for every page.
- **`list`** (and other list-like types) are used for index/collection pages. In list mode, Lunet injects `pages = site.pages` into the template scope.
- List types are rendered after all single pages (ordered by weight in `site.layout_types`).

Modules can register custom layout types (e.g. `rss`, `sitemap`, `api-dotnet`, `term`, `terms`).

## Include files

### Where includes live

Include templates live under `/.lunet/includes/` in the meta filesystem. Like layouts, they can come from your site, a theme, or Lunet’s built-in shared files.

### Using includes

```scriban
{{ '{{' }} include "partials/nav.sbn-html" {{ '}}' }}
{{ '{{' }} include "_builtins/head.sbn-html" {{ '}}' }}
```

Includes resolve paths relative to `/.lunet/includes/`. You cannot use:
- absolute paths starting with `/` or `\`
- parent traversal (`..`)

### Built-in includes

Lunet and its modules ship several built-in includes under `_builtins/`:

- `_builtins/head.sbn-html` — standard `<head>` content
- `_builtins/bundle.sbn-html` — CSS/JS bundle injection
- `_builtins/cards.sbn-html` — OpenGraph/Twitter meta tags

Themes typically include these in their base layout. You can override any built-in by creating the same path under your site’s `/.lunet/includes/`.

## Template variables reference

### Top-level variables

{.table}
| Variable | Type | Availability | Description |
|---|---|---|---|
| `site` | SiteObject | All templates and front matter | Global site state and module configuration |
| `page` | ContentObject | Page and layout rendering | Current page metadata and content |
| `content` | string | Layout rendering only | The inner content being wrapped |
| `pages` | PageCollection | List layouts only | Shortcut to `site.pages` |

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
| `site.data` | object | Data loaded from `/.lunet/data/` |
| `site.html` | object | HTML head/body configuration |
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
| `site.builtins.xref(uid)` | Resolve a UID to `{ url, name, fullname, page? }` |

### `site.html.*`

{.table}
| Field | Type | Description |
|---|---|---|
| `site.html.attributes` | string | Attributes injected on `<html>` |
| `site.html.head.title` | string/template | Custom `<title>` override |
| `site.html.head.metas` | collection | `<meta>` entries rendered in `<head>` |
| `site.html.head.includes` | collection | Include templates rendered in `<head>` |
| `site.html.body.attributes` | string | Attributes injected on `<body>` |
