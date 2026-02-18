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

Lunet checks the first 3 bytes of each file (after an optional UTF-8 BOM) to detect front matter. The Scriban parser (`+++`) is checked before the YAML parser (`---`).

#### YAML front matter (`---`)

The most common format. Requires the [YAML module](plugins/yaml.md) (enabled by default).

```markdown
---
title: "Hello"
layout: docs
tags: ["tutorial", "getting-started"]
---

# Hello

This is a Markdown page.
```

All key-value pairs from the YAML block are assigned to the page object. YAML arrays and maps become Scriban arrays and objects.

#### Scriban front matter (`+++`)

Scriban front matter is always available (no module required) and supports executable Scriban code:

```text
+++
layout = "docs"
title = "Hello"
my_custom_var = "computed-" + site.environment
+++
<h1>Hello</h1>
```

Because the context is the page object, `title = "Hello"` sets `page.title`, just like YAML front matter. Within `+++` blocks, you have access to `site` (read-only) and the full Scriban language including conditionals and loops.

### Front matter evaluation order

When loading a page:

1. The [Attributes module](plugins/attributes.md) applies glob-matched defaults first (e.g. URL patterns for date-based filenames).
2. Front matter is evaluated, overriding any attribute defaults.
3. URL computation runs (section, slug, folder URLs, basepath).
4. The page body is evaluated as a Scriban template.

This means attribute defaults act as fallbacks — front matter always wins.

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
{{ '{{' }} page.title {{ '}}' }}           # "My Page" (from front matter)
{{ '{{' }} site.title {{ '}}' }}           # site title (from config.scriban)
{{ '{{' }} page.url {{ '}}' }}             # computed URL for this page
{{ '{{' }} page.section {{ '}}' }}         # first directory segment, e.g. "docs"
{{ '{{' }} page.custom_flag {{ '}}' }}     # true (custom front matter key)
{{ '{{' }} site.environment {{ '}}' }}     # "prod" or "dev"
```

> [!TIP]
>
> Any key you add to front matter becomes accessible as `page.<key>`. You are not limited to the built-in fields.

## Page variables reference

### Core fields (set in front matter or computed)

{.table}
| Field | Type | Default | Description |
|---|---|---|---|
| `page.title` | string | `null` (or parsed from date-filename) | Page title. For date-based filenames, derived by capitalizing the slug. |
| `page.layout` | string | section name (e.g. `"docs"`) | Layout name. Defaults to `page.section`, i.e. pages in `docs/` look for a `docs` layout. |
| `page.layout_type` | string | `"single"` | Layout type (`single`, `list`, or custom). |
| `page.layout_content_type` | string | `null` | Override content type for layout lookup (e.g. set to `"xml"` to use an XML layout). |
| `page.url` | string | Computed from path | Output URL with `site.basepath` applied. Writable from front matter. |
| `page.url_without_basepath` | string | Computed from path | URL without `site.basepath` prefix. |
| `page.slug` | string | Handleized title or filename part | URL slug. Derived from title via `string.handleize` if not set explicitly. |
| `page.date` | datetime | `DateTime.Now` (or parsed from filename) | Page date. For date-based filenames (e.g. `2024-01-15-my-post.md`), extracted automatically. Accepts string values — Lunet parses them with `DateTime.TryParse`. |
| `page.weight` | int | Auto-assigned (10, 20, 30…) | Sorting hint used by menus and templates. Auto-assigned by alphabetical file order within each directory if not set in front matter. |
| `page.uid` | string | `null` | Stable identifier for `xref`/`ref`/`relref` lookups. |
| `page.content` | string | (loaded on demand) | Page body content after template evaluation. |
| `page.content_type` | string | Derived from extension | Output content type (`html`, `md`, `xml`, `css`, etc.). Read-only — set by the content type manager based on file extension. Updated when converters run (e.g. Markdown → HTML). |
| `page.summary` | string | Auto-computed by [Summarizer](plugins/summarizer.md) | Summary text. Can be overridden in front matter. |
| `page.discard` | bool | `false` | When `true`, page is processed but not written to output. |

### Read-only fields (computed from source file)

{.table}
| Field | Type | Description |
|---|---|---|
| `page.path` | string | Source file path relative to site root (e.g. `/docs/intro.md`). |
| `page.ext` | string | Source file extension including dot (e.g. `".md"`), lowercase. |
| `page.section` | string | First directory segment of the source path (e.g. `"docs"`). Empty string for root-level files. |
| `page.path_in_section` | string | Remaining path after the section directory (e.g. `/intro.md` within section `docs`). |
| `page.length` | long | Source file size in bytes. |
| `page.modified_time` | datetime | Source file modification time (max of creation and last-write time). |

### Summarizer fields (per-page overrides)

These can be set in front matter to override site-level summarizer defaults:

{.table}
| Field | Type | Default | Description |
|---|---|---|---|
| `page.summary_word_count` | int | `70` | Maximum words in auto-generated summary. Falls back to `site.summary_word_count`. |
| `page.summary_keep_formatting` | bool | `false` | Keep HTML formatting in summary. Falls back to `site.summary_keep_formatting`. |

See the [Summarizer module](plugins/summarizer.md) for details.

### Common front matter keys (used by modules)

These aren't built-in fields but are used by convention across modules:

{.table}
| Key | Type | Used by | Description |
|---|---|---|---|
| `tags` | array | [Taxonomies](plugins/taxonomies.md) | Tag list for the page |
| `categories` | array | [Taxonomies](plugins/taxonomies.md) | Category list for the page |
| `bundle` | string | [Bundles](plugins/bundles.md) | Which bundle to inject for this page |
| `sitemap` | bool | [Sitemaps](plugins/sitemaps.md) | Set `false` to exclude from sitemap |
| `sitemap_priority` | float | [Sitemaps](plugins/sitemaps.md) | Sitemap priority (0.0–1.0) |
| `sitemap_changefreq` | string | [Sitemaps](plugins/sitemaps.md) | Sitemap change frequency |
| `og_type` | string | [Cards](plugins/cards.md) | OpenGraph type override |
| `summary` | string | [Summarizer](plugins/summarizer.md), [RSS](plugins/rss.md), [Cards](plugins/cards.md) | Manual summary (overrides auto-computed) |

### xref fields (advanced)

{.table}
| Field | Type | Description |
|---|---|---|
| `page.xref_name` | string | Short name returned by `xref` lookups. Primarily used by the [API .NET module](plugins/api-dotnet.md). |
| `page.xref_fullname` | string | Full qualified name for `xref` lookups. |

## Pages vs static files

{.table}
| File | Has front matter? | Result |
|---|---|---|
| `docs/intro.md` with `---` block | Yes | Page → layout applied → HTML output |
| `img/logo.svg` | No | Static file → copied as-is |
| `css/main.scss` | No | Converted by [SCSS module](plugins/scss.md) → CSS output |
| `data/feed.sbn-xml` | Yes (Scriban template) | Page → evaluated as Scriban → XML output |

## URL behavior

If you don't set `url` explicitly in front matter, Lunet derives the output URL from the file path.

### Folder URLs (default)

By default, pages with HTML output get "folder URLs":

- `docs/intro.md` → `/docs/intro/` (written as `/docs/intro/index.html`)
- `about.md` → `/about/`

Special cases:

- `index.*` maps to the parent folder: `docs/index.md` → `/docs/`
- `readme.*` behaves like `index.*` when `site.readme_as_index = true` (the default). The comparison is case-insensitive.

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

### URL placeholders

The `url` field supports placeholders that are replaced at URL computation time. This is most useful with the [Attributes module](plugins/attributes.md), which can set URL patterns by glob match:

{.table}
| Placeholder | Value | Example |
|---|---|---|
| `:title` | Handleized title | `my-page` |
| `:slug` | Page slug | `my-post` |
| `:section` | Section name | `blog` |
| `:year` | 4-digit year | `2024` |
| `:month` | 2-digit month | `01` |
| `:day` | 2-digit day | `15` |
| `:output_ext` | Output extension | `.html` |

Additional time placeholders: `:short_year`, `:i_month`, `:short_month`, `:long_month`, `:i_day`, `:y_day`, `:w_year`, `:week`, `:w_day`, `:short_day`, `:long_day`, `:hour`, `:minute`, `:second`.

See the [Attributes module](plugins/attributes.md) for full placeholder documentation and glob matching.

## Scriban file types

Certain file extensions are treated as Scriban templates (even without `---` or `+++` front matter delimiters):

- `*.sbn-html`, `*.sbn-md`, `*.sbn-xml`, `*.sbn-js`, `*.sbn-css`, etc.
- `*.scriban-html`, `*.scriban-md`, `*.scriban-xml` — long-form equivalents

These are evaluated as Scriban templates and produce the corresponding output content type. This is useful for files that need Scriban logic but aren't Markdown pages (e.g., a dynamic XML sitemap or RSS feed).

Lunet also recognizes `.markdown` as an alias for `.md`.

## Date-based filenames

Lunet recognizes filenames matching the pattern `YYYY-MM-DD-title.ext`:

- `2024-01-15-my-post.md` → sets `page.date` to `2024-01-15`, `page.slug` to `my-post`, and `page.title` to `My Post`

The [Attributes module](plugins/attributes.md) registers a default glob rule for date-based filenames that sets the URL pattern to `/:section/:year/:month/:day/:slug:output_ext`. This means a file like `blog/2024-01-15-my-post.md` automatically gets the URL `/blog/2024/01/15/my-post/`.

## Sections

The first directory segment of a content file's path is its **section**:

- `docs/intro.md` → section = `docs`
- `blog/2024-01-01-hello.md` → section = `blog`
- `readme.md` → section = `""` (root, no section)

Sections matter because:

1. `page.layout` defaults to the section name — pages in `docs/` look for a `docs` layout first.
2. You can organize layouts by section (`/.lunet/layouts/docs.sbn-html`, `/.lunet/layouts/blog.sbn-html`).
3. [RSS feeds](plugins/rss.md) and [taxonomies](plugins/taxonomies.md) can filter by section.

## See also

- [Configuration (`config.scriban`)](configuration.md) — site-level context vs page-level context
- [Layouts & includes](layouts-and-includes.md) — how `page.layout` and `page.layout_type` drive template selection
- [Attributes module](plugins/attributes.md) — URL patterns, glob-based metadata, date-based defaults
- [Summarizer module](plugins/summarizer.md) — auto-generated summaries and `<!-- more -->` markers
- [Taxonomies module](plugins/taxonomies.md) — tags and categories in front matter
- [Markdown module](plugins/markdown.md) — Markdown to HTML conversion
