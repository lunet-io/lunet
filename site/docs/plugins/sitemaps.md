---
title: "Sitemaps module"
---

# Sitemaps module

The sitemaps module automatically generates a `/sitemap.xml` and a `/robots.txt` for your site. It is **enabled by default** — no configuration is needed.

## What gets generated

{.table}
| File | Condition | Content |
|---|---|---|
| `/sitemap.xml` | Always (unless you provide your own) | Standard XML sitemap with all HTML pages |
| `/robots.txt` | Only if no `/robots.txt` exists in your content | Contains `Sitemap: <absolute-url-to-sitemap.xml>` |

If you provide your own `/sitemap.xml` file, the module uses it as the base page and injects the collected URLs into it via the `urls` template variable.

If you provide your own `/robots.txt`, the module will not generate one.

## Per-page settings

By default, all HTML pages are included in the sitemap. You can exclude individual pages or set optional sitemap fields in front matter:

```yaml
sitemap: false
```

Optional fields:

```yaml
sitemap_priority: 0.8
sitemap_changefreq: weekly
```

{.table}
| Front matter key | Type | Default | Description |
|---|---|---|---|
| `sitemap` | bool | `true` | Set to `false` to exclude this page from the sitemap |
| `sitemap_priority` | float | *(not set)* | Maps to the `<priority>` element (0.0 to 1.0) |
| `sitemap_changefreq` | string | *(not set)* | Maps to the `<changefreq>` element (e.g. `always`, `hourly`, `daily`, `weekly`, `monthly`, `yearly`, `never`) |

## Sitemap entry fields

Each entry in the generated sitemap includes:

{.table}
| XML element | Source | Notes |
|---|---|---|
| `<loc>` | Page absolute URL | Resolved via URL reference system |
| `<lastmod>` | File modification time | Falls back to current date if unavailable |
| `<changefreq>` | `sitemap_changefreq` front matter | Only included if set |
| `<priority>` | `sitemap_priority` front matter | Only included if set |

## Customizing the layout

The sitemap is rendered using the layout file `_default.sitemap.xml`. You can override it by placing your own version at `.lunet/layouts/_default.sitemap.xml` in your site. The layout receives a `urls` collection where each entry has `loc`, `lastmod`, `changefreq`, and `priority` properties.

## See also

- [RSS module](rss.md) — RSS feed generation
- [Attributes module](attributes.md) — URL patterns that affect sitemap URLs
