---
title: "Content & front matter"
---

# Content & front matter

Lunet treats files as either:
- **pages** (templates with front matter, processed by the pipeline), or
- **static files** (copied as-is).

## Front matter formats

### YAML front matter (`---`)

YAML front matter is enabled when the YAML module is registered (it is by default).

```markdown
---
title: "Hello"
layout: docs
---

# Hello
```

### Scriban front matter (`+++`)

Scriban front matter is always available and can run Scriban statements:

```text
+++
layout = "docs"
title = "Hello"
+++
<h1>{{title}}</h1>
```

## Useful page variables

These can be set in front matter:

- `title` — page title
- `layout` — layout name (defaults to your section name, then `_default`)
- `layout_type` — layout type (`single` by default; some modules add custom list types)
- `url` — override the output URL
- `discard` — when `true`, do not write the page to output
- `bundle` — choose which bundle is injected by the bundle include
- `tags`, `categories` — taxonomies (arrays of strings)
- `sitemap` — set `false` to exclude from sitemap generation

Other commonly used fields:

- `summary` — short description used by RSS/cards (otherwise computed by the summarizer)
- `uid` — stable identifier used by xref/linking systems
- `weight` — ordering hint used by some templates

## Pages vs static files

In general:
- Files with front matter become **pages**.
- Files without front matter become **static files**, unless a converter claims them.

Examples:
- `docs/intro.md` with YAML front matter → page → layout applied.
- `img/logo.svg` → static file copy.
- `css/main.scss` → converted to CSS by the SCSS module.

## URL behavior (important)

If you don’t set `url` explicitly, Lunet derives it from the file path.

### Folder URLs (default)

When a file:
- has front matter (so it is a page),
- and its output is HTML-like,
- and `site.url_as_file` is `false`,

then Lunet will turn the file into a “folder URL”:

- `docs/intro.md` → `/docs/intro/` (written as `/docs/intro/index.html`)

Special cases:
- `index.*` always maps to the parent folder: `docs/index.md` → `/docs/`
- `readme.*` behaves like `index.*` when `site.readme_as_index = true`

### File URLs

Set `site.url_as_file = true` to keep `*.html` in URLs:

```scriban
url_as_file = true
```

## Scriban file types

Lunet treats some file extensions as Scriban templates:

- `*.sbn-html`, `*.sbn-md`, `*.sbn-xml`, `*.sbn-js`, `*.sbn-css`, …

These map to the corresponding output content type.

