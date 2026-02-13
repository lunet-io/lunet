---
title: "Summarizer module"
---

# Summarizer module

The summarizer module computes `page.summary` for pages.

It runs after layout processing so it can summarize the final HTML.

This is used by:
- RSS layouts (feed item descriptions)
- Cards meta tags (`twitter:description`, `og:description`)

## Basic usage

No configuration is required. If you want per-page control, set `summary` explicitly in front matter:

```yaml
summary: "Short description for feeds and social cards."
```

