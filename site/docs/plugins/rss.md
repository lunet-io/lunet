---
title: "RSS module"
---

# RSS module

The RSS module provides RSS 2.0 feed generation driven by the layout system. It registers `layout_type = "rss"` and ships a default RSS layout template.

## Create a feed page

Add a page like `rss.xml` with front matter that selects the RSS layout:

```xml
---
title: "RSS"
layout: "_default"
layout_type: "rss"
sitemap: false
---
```

The default layout renders items from `site.pages.by_date` (newest first), filtered to only include content pages (pages without a `layout_type`, or with `layout_type = "single"`). List pages, RSS pages, sitemap pages, and other generated pages are automatically excluded.

## Configure limits

The default limit is **10** items per feed. Override it site-wide in `config.scriban`:

```scriban
site.rss.limit = 20
```

Or per feed page in front matter:

```yaml
rss_limit: 50
```

{.table}
| Property | Scope | Type | Default | Description |
|---|---|---|---|---|
| `site.rss.limit` | site | int | `10` | Maximum number of items in all RSS feeds |
| `rss_limit` | page | int | *(site limit)* | Override the limit for this specific feed |
| `rss_section` | page | string | *(none)* | Only include posts from the named section |

## Section-specific feeds

Create multiple feed pages for different sections:

```xml
---
title: "Blog RSS"
layout: "_default"
layout_type: "rss"
rss_section: "blog"
sitemap: false
---
```

This feed will only include pages whose `section` matches `"blog"`.

## Channel metadata

The RSS channel draws metadata from site-level properties:

{.table}
| RSS element | Source |
|---|---|
| `<title>` | `site.title` |
| `<description>` | `site.description` |
| `<language>` | `site.language` |
| `<managingEditor>` | `site.author` |
| `<copyright>` | `site.copyright` (falls back to `site.author`) |
| `<generator>` | `lunet <version>` (automatic) |
| `<pubDate>` | Build time |
| `<lastBuildDate>` | Build time |

An Atom self-link (`<atom:link rel="self">`) is also included for RSS best practices.

## Item content

Each feed item includes:

{.table}
| RSS element | Source |
|---|---|
| `<title>` | `post.title` |
| `<link>` | Absolute URL of the page |
| `<pubDate>` | Page date (RFC 822 format) |
| `<author>` | `post.author` or `site.author` |
| `<guid>` | Permalink URL |
| `<description>` | `post.summary` (from the [Summarizer module](summarizer.md)) |
| `<category>` | All taxonomy terms assigned to the page |

## Customizing the layout

The RSS feed is rendered using `_default.rss.xml`. You can override it by placing your own version at `.lunet/layouts/_default.rss.xml` in your site.

## See also

- [Summarizer module](summarizer.md) — provides `page.summary` for feed item descriptions
- [Sitemaps module](sitemaps.md) — XML sitemap generation (set `sitemap: false` on feed pages)
- [Taxonomies module](taxonomies.md) — taxonomy terms appear as RSS `<category>` elements
