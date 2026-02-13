---
title: "RSS module"
---

# RSS module

RSS output is driven by layouts. The RSS module registers `layout_type = "rss"` and ships a default RSS layout.

## Create a feed page

Add a page like `rss.xml`:

```xml
---
title: "RSS"
layout: "_default"
layout_type: "rss"
sitemap: false
---
```

The default layout renders items from `site.pages.by_date`.

## Configure limits

Global limit:

```scriban
site.rss.limit = 20
```

Per-feed:

```yaml
rss_limit: 50
```

Optional filtering:
- `rss_section` (front matter) to keep posts from a specific section

