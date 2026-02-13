---
title: "Taxonomies module"
---

# Taxonomies module

Taxonomies provide many-to-many grouping like tags and categories.

Defaults:
- `tags` / `tag`
- `categories` / `category`

## Define taxonomies

In `config.scriban`:

```scriban
with taxonomies
  tags = "tag"
  categories = "category"
  series = { singular: "serie", url: "/series/" }
end
```

## Tag a page

In page front matter:

```yaml
tags: ["lunet", "docs"]
categories: ["guides"]
```

The taxonomy module generates dynamic pages:
- one page per term
- one page for the list of terms

You control rendering by providing layouts named after the taxonomy (e.g. `tags.term.*`, `tags.terms.*`), or by using theme defaults.

