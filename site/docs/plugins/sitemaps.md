---
title: "Sitemaps module"
---

# Sitemaps module

When enabled, the sitemaps module generates:
- `/sitemap.xml`
- `/robots.txt` (if you don’t provide one)

## Enable/disable

Enabled by default:

```scriban
site.sitemaps.enable = true
```

Disable:

```scriban
site.sitemaps.enable = false
```

## Per-page settings

Exclude a page from sitemap:

```yaml
sitemap: false
```

Optional fields:

```yaml
sitemap_priority: 0.5
sitemap_changefreq: weekly
```

