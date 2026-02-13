---
title: "Cards module (OpenGraph/Twitter)"
---

# Cards module (OpenGraph/Twitter)

The cards module injects social/SEO meta tags into your `<head>`.

## Enable

```scriban
with cards
  with twitter
    enable = true
    card = "summary_large_image"
    user = "yourhandle"
    image = "/img/social.png"
  end
  with og
    enable = true
    type = "website"
    image = "/img/social.png"
  end
end
```

## Per-page overrides

In page front matter:

```yaml
og_type: website
twitter_title: "Custom title"
twitter_image: "/img/other.png"
```

The module relies on `page.summary` (from the summarizer module) when available.

