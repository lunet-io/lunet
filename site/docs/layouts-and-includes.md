---
title: "Layouts & includes"
---

# Layouts & includes

Layouts and includes are evaluated when Lunet renders pages, not while loading `config.scriban`.

## Configuration vs template runtime

{.table}
| Context | Main purpose | `include` availability | Main objects |
|---|---|---|---|
| `config.scriban` | Configure site and modules | Not allowed | `site`, module root objects (`bundle`, `scss`, `taxonomies`, …) |
| Page/layout templates | Render content | Allowed from `/.lunet/includes/**` | `site`, `page`, `content`, and sometimes `pages` |

Use config for defaults and switches:

```scriban
layout = "_default"
site.html.head.includes.add "_builtins/cards.sbn-html"
```

Use templates for HTML generation:

```scriban
{{ include "partials/nav.sbn-html" }}
<main>{{ content }}</main>
```

## Include files (`/.lunet/includes/**`)

`include` always resolves from `/.lunet/includes`.

```scriban
{{ include "_builtins/head.sbn-html" }}
```

Security rules:
- paths must be relative
- paths cannot start with `/` or `\`
- paths cannot contain `..`

## Layout files (`/.lunet/layouts/**`)

The layout processor resolves each page using:
- `layout` name
- `layout_type`
- current page content type (for example `html`, `xml`, `rss`, `sitemap`)

Resolution starts from:
- `page.layout` (or section name)
- `page.layout_type` (default `single`)
- `page.content_type`

If no layout is found, Lunet may convert the content type (for example Markdown -> HTML), then resolve again with the new type.

### Search patterns

For each candidate path, Lunet tries all registered extensions for the target content type (for example `.sbn-html`, `.scriban-html`, `.html`).

{.table}
| `layout_type` | Paths tried (in order), under `/.lunet/layouts` |
|---|---|
| `single` | `<layout>/<type>`, `<layout>.<type>`, `<layout>`, `_default/<type>`, `_default` |
| `list` and other list-like types | `<layout>/<type>`, `<layout>.<type>`, `_default/<type>`, `_default.<type>` |

Examples:
- `/.lunet/layouts/_default.sbn-html`
- `/.lunet/layouts/docs.single.sbn-html`
- `/.lunet/layouts/docs.list.sbn-html`
- `/.lunet/layouts/tags.term.sbn-html`
- `/.lunet/layouts/_default.rss.xml`

### Layout chaining and recursion guard

A layout can select a next layout from its front matter:
- `layout`
- `layout_type`
- `layout_content_type`

Lunet re-runs layout resolution with the new tuple. If the same tuple is visited twice, Lunet stops with a recursive-layout error.

### Single vs list rendering

- `single` is the normal page rendering mode.
- list-like types are rendered after singles (ordered by `site.layout_types` weight).
- in list mode, Lunet injects `pages = site.pages` into template scope.

## Template variables

### Top-level variables

{.table}
| Variable | Type | Availability | Description |
|---|---|---|---|
| `site` | `SiteObject` | all template/front matter evaluations | Global site state and module configuration |
| `page` | `ContentObject` | page and layout rendering | Current page metadata and content |
| `content` | `string` | layout rendering | Current inner content being wrapped |
| `pages` | `PageCollection` | list layouts only | Shortcut to `site.pages` |

### `page.*` (core fields)

{.table}
| Field | Type | Read/Write | Description |
|---|---|---|---|
| `page.title` | `string` | read/write | Page title |
| `page.slug` | `string` | read/write | URL slug (defaults from title) |
| `page.date` | `datetime` | read/write | Page date |
| `page.weight` | `int` | read/write | Sorting hint |
| `page.uid` | `string` | read/write | Stable id for xref/ref lookups |
| `page.url` | `string` | read/write | URL with `site.basepath` applied |
| `page.url_without_basepath` | `string` | read/write | URL without `site.basepath` |
| `page.content` | `string` | read/write | Current page body (post-processor input/output) |
| `page.summary` | `string` | read/write | Summary text |
| `page.summary_keep_formatting` | `bool` | read/write | Summarizer hint to preserve formatting |
| `page.summary_word_count` | `int` | read/write | Summarizer target word count |
| `page.content_type` | `string` | read-only in templates | Current output type (`html`, `xml`, …) |
| `page.layout` | `string` | read/write | Layout name |
| `page.layout_type` | `string` | read/write | Layout type (`single`, `list`, module-specific) |
| `page.layout_content_type` | `string` | read/write | Override next layout content type in chains |
| `page.discard` | `bool` | read/write | Skip output when `true` |
| `page.xref_name` | `string` | read/write | Short xref name used by API docs |
| `page.xref_fullname` | `string` | read/write | Full xref name used by API docs |
| `page.section` | `string` | read-only | First directory segment of source path |
| `page.path_in_section` | `string` | read-only | Source path without section prefix |
| `page.path` | `string` | read-only | Source file path |
| `page.ext` | `string` | read-only | Source extension |
| `page.length` | `int64` | read-only | Source file length |
| `page.modified_time` | `datetime` | read-only | Source file modified time |

Any extra front matter keys are also available on `page` (for example `tags`, `categories`, `menu`, custom flags).

### `site.*` (core fields)

{.table}
| Field | Type | Read/Write | Description |
|---|---|---|---|
| `site.baseurl` | `string` | read/write | Canonical host URL |
| `site.basepath` | `string` | read/write | URL prefix for hosted sub-paths |
| `site.baseurlforce` | `bool` | read/write | Prevent `serve` from overriding base URL/path |
| `site.environment` | `string` | read/write | Build environment (`dev` or `prod`) |
| `site.layout` | `string` | read/write | Default layout fallback |
| `site.url_as_file` | `bool` | read/write | Keep `*.html` in URL instead of folder URL |
| `site.default_page_ext` | `string` | read/write | Default HTML output extension |
| `site.readme_as_index` | `bool` | read/write | Treat `readme.*` as `index.*` |
| `site.error_redirect` | `string` | read/write | Default error redirection target |
| `site.pages` | `PageCollection` | read-only reference | Loaded page list |
| `site.includes` | glob collection | read/write | Include override globs |
| `site.excludes` | glob collection | read/write | Exclude globs |
| `site.force_excludes` | glob collection | read-only reference | Hard exclusions |
| `site.layout_types` | map | read-only reference | Weight map controlling layout-type order |
| `site.html` | object | read/write | HTML head/body configuration object |
| `site.builtins` | object | read-only reference | Built-in helpers/functions |

Modules add their own root objects on `site` (for example `site.data`, `site.taxonomies`, `site.search`, `site.rss`).

### `site.html.*`

{.table}
| Field | Type | Description |
|---|---|---|
| `site.html.attributes` | `string` | Attributes injected on `<html ...>` |
| `site.html.head.title` | `string`/template | Optional custom `<title>` |
| `site.html.head.metas` | collection | `<meta ...>` entries rendered by `Head` |
| `site.html.head.includes` | collection | Include templates rendered in `<head>` |
| `site.html.body.attributes` | `string` | Attributes injected on `<body ...>` |

### `site.pages` helpers

{.table}
| Helper | Result |
|---|---|
| `site.pages.by_weight` | new collection ordered by `weight`, then date |
| `site.pages.by_date` | new collection ordered by date |
| `site.pages.by_length` | new collection ordered by source length |
| `site.pages.by_title` | new collection ordered by title |

### `site.builtins.*` (commonly used)

{.table}
| Helper | Description |
|---|---|
| `site.builtins.log.info/warn/error/debug/trace/fatal` | Emit logs from templates/config |
| `site.builtins.lunet.version` | Current Lunet version |
| `site.builtins.defer(expr)` | Defer expression evaluation to end of processing |
| `site.builtins.ref(url)` | Resolve an absolute URL using site routing |
| `site.builtins.relref(url)` | Resolve a relative URL from current page |
| `site.builtins.xref(uid)` | Resolve an xref UID to `{ url, name, fullname, page? }` |
