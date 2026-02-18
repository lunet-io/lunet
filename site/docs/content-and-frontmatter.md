---
title: "Content & front matter"
---

# Content & front matter

Lunet treats files in your site folder as either:

- **Pages** — files with front matter, processed through the template and layout pipeline.
- **Static files** — files without front matter, copied to the output as-is (unless a converter like SCSS claims them).

## How front matter works

Front matter is a metadata block at the top of a file. When Lunet detects front matter, the file becomes a **page**.

The key concept: **front matter runs with the page as its scripting context.** This means that any variable you set in front matter is set on the `page` object, not on `site`.

```yaml
---
title: "Hello"        # sets page.title
layout: docs           # sets page.layout
---
```

This is the opposite of `config.scriban`, where variables are set on `site`. See [Configuration](configuration.md) for more details.

### Front matter formats

#### YAML front matter (`---`)

The most common format. Requires the YAML module (enabled by default).

```markdown
---
title: "Hello"
layout: docs
tags: ["tutorial", "getting-started"]
---

# Hello

This is a Markdown page.
```

#### Scriban front matter (`+++`)

Scriban front matter is always available and supports executable Scriban code:

```text
+++
layout = "docs"
title = "Hello"
my_custom_var = "computed-" + site.environment
+++
<h1>{{ page.title }}</h1>
```

Because the context is the page object, `title = "Hello"` sets `page.title`, just like YAML front matter.

## Using variables in page templates

After front matter is processed, the page body is evaluated as a Scriban template. In this context, two key objects are available:

- **`page`** — the current page (metadata from front matter + computed fields).
- **`site`** — the global site object (configuration, pages collection, modules).

### Example: accessing page and site variables

Given this front matter:

```yaml
---
title: "My Page"
custom_flag: true
---
```

And this template body:

```scriban
{{ '{{' }} page.title {{ '}}' }}           {{!-- "My Page" (from front matter) --}}
{{ '{{' }} site.title {{ '}}' }}           {{!-- site title (from config.scriban) --}}
{{ '{{' }} page.url {{ '}}' }}             {{!-- computed URL for this page --}}
{{ '{{' }} page.section {{ '}}' }}         {{!-- first directory segment, e.g. "docs" --}}
{{ '{{' }} page.custom_flag {{ '}}' }}     {{!-- true (custom front matter key) --}}
{{ '{{' }} site.environment {{ '}}' }}     {{!-- "prod" or "dev" --}}
```

> [!TIP]
>
> Any key you add to front matter becomes accessible as `page.<key>`. You are not limited to the built-in fields.

## Page variables reference

### Core fields (set in front matter or computed)

{.table}
| Field | Type | Description |
|---|---|---|
| `page.title` | string | Page title |
| `page.layout` | string | Layout name (defaults to section name, then `_default`) |
| `page.layout_type` | string | Layout type (`single` by default) |
| `page.url` | string | Output URL (with `site.basepath` applied) |
| `page.url_without_basepath` | string | URL without `site.basepath` |
| `page.slug` | string | URL slug (derived from title if not set) |
| `page.date` | datetime | Page date (can be parsed from filename `YYYY-MM-DD-title.ext`) |
| `page.weight` | int | Sorting hint used by menus and templates |
| `page.uid` | string | Stable identifier for xref/ref lookups |
| `page.section` | string | First directory segment of the source path (read-only) |
| `page.content` | string | Page body content (post-processing) |
| `page.content_type` | string | Output content type (`html`, `xml`, etc.) |
| `page.summary` | string | Summary text (auto-computed by summarizer or set manually) |
| `page.discard` | bool | When `true`, page is processed but not written to output |
| `page.path` | string | Source file path (read-only) |
| `page.ext` | string | Source file extension (read-only) |

### Common front matter keys

These aren't built-in fields but are used by convention across modules:

{.table}
| Key | Type | Used by | Description |
|---|---|---|---|
| `tags` | array | Taxonomies | Tag list for the page |
| `categories` | array | Taxonomies | Category list for the page |
| `bundle` | string | Bundles | Which bundle to inject for this page |
| `sitemap` | bool | Sitemaps | Set `false` to exclude from sitemap |
| `sitemap_priority` | float | Sitemaps | Sitemap priority (0.0–1.0) |
| `sitemap_changefreq` | string | Sitemaps | Sitemap change frequency |
| `og_type` | string | Cards | OpenGraph type override |
| `summary` | string | Summarizer, RSS, Cards | Manual summary (overrides auto-computed) |

## Pages vs static files

{.table}
| File | Has front matter? | Result |
|---|---|---|
| `docs/intro.md` with `---` block | Yes | Page → layout applied → HTML output |
| `img/logo.svg` | No | Static file → copied as-is |
| `css/main.scss` | No | Converted by SCSS module → CSS output |
| `data/feed.sbn-xml` | Yes (Scriban template) | Page → evaluated as Scriban → XML output |

## URL behavior

If you don’t set `url` explicitly in front matter, Lunet derives the output URL from the file path.

### Folder URLs (default)

By default, pages with HTML output get "folder URLs":

- `docs/intro.md` → `/docs/intro/` (written as `/docs/intro/index.html`)
- `about.md` → `/about/`

Special cases:

- `index.*` maps to the parent folder: `docs/index.md` → `/docs/`
- `readme.*` behaves like `index.*` when `site.readme_as_index = true`

### File URLs

Set `site.url_as_file = true` in `config.scriban` to keep `*.html` in URLs:

```scriban
url_as_file = true
```

With this setting: `docs/intro.md` → `/docs/intro.html`

### Overriding the URL

Set `url` in front matter to use a custom output path:

```yaml
---
title: "Custom URL page"
url: /custom/path/
---
```

## Scriban file types

Certain file extensions are treated as Scriban templates (even without `---` or `+++` front matter delimiters):

- `*.sbn-html`, `*.sbn-md`, `*.sbn-xml`, `*.sbn-js`, `*.sbn-css`, etc.

These are evaluated as Scriban templates and produce the corresponding output content type. This is useful for files that need Scriban logic but aren’t Markdown pages (e.g., a dynamic XML sitemap or RSS feed).

## Date-based filenames

Lunet recognizes filenames with dates:

- `2024-01-15-my-post.md` → sets `page.date` to `2024-01-15`, `page.slug` to `my-post`, and `page.title` to `My Post`

This is a convention for blog-style content. The date can then be used in URL patterns via the [Attributes module](plugins/attributes.md).
