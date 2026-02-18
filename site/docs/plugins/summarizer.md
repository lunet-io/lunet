---
title: "Summarizer module"
---

# Summarizer module

The summarizer module automatically computes `page.summary` — a plain-text excerpt of each page's content. Summaries are used by the RSS module for feed descriptions and by the Cards module for social media meta tags (`og:description`, `twitter:description`).

## How it works

The summarizer runs **before** layout processing, so it summarizes the rendered content body (after Markdown/Scriban evaluation) but before it is wrapped in a layout template.

Only pages with **HTML content type** are processed. Markdown, CSS, JavaScript, and other content types are skipped.

The algorithm:

1. Parses the page's rendered HTML content.
2. If a `<!-- lunet:summarize -->` comment is present, skips all text before it.
3. If a `<!-- more -->` comment is present, uses only the text before it as the summary.
4. Otherwise, extracts plain text and truncates to `summary_word_count` words (default: 70), adding an ellipsis (`…`).
5. Assigns the result to `page.summary`.

## Configuration

{.table}
| Property | Scope | Type | Default | Description |
|---|---|---|---|---|
| `summary_word_count` | page or site | int | `70` | Maximum number of words in the summary |
| `summary_keep_formatting` | page or site | bool | `false` | When `true`, preserves some HTML formatting in the extracted text |

Properties are looked up on the page first, then fall back to the site object.

Set site-wide defaults in `config.scriban`:

```scriban
summary_word_count = 100
summary_keep_formatting = false
```

Or override per page in front matter:

```yaml
summary_word_count: 50
```

## Content markers

### The `<!-- more -->` comment

Insert a `<!-- more -->` comment in your content to manually control where the summary ends. All text before the marker becomes the summary, and the word count limit is **not** applied:

```markdown
This is the introduction that will appear in feeds and social cards.

<!-- more -->

This content will not appear in the summary.
```

### The `<!-- lunet:summarize -->` comment

Insert a `<!-- lunet:summarize -->` comment to mark where the summarizable content **begins**. All text before this marker is excluded from the summary. This is useful when pages start with navigation, hero sections, or other non-content elements:

```markdown
<div class="hero">Welcome to my site</div>

<!-- lunet:summarize -->

This is the actual content that should be summarized.
```

Both markers can be combined: text before `<!-- lunet:summarize -->` is skipped, and text after `<!-- more -->` is excluded. The summary becomes the content between the two markers.

## Edge cases

- Only HTML content types are processed. Non-HTML pages (Markdown source before rendering, CSS, JS) are skipped silently.
- Negative `summary_word_count` values are treated as the default (70).
- The summarizer always overwrites `page.summary`, even if set in front matter. To provide a manual summary for an HTML page, the page must be processed differently (e.g. non-HTML content type).

## See also

- [RSS module](rss.md) — uses `page.summary` for feed item descriptions
- [Cards module](cards.md) — uses `page.summary` for social media meta tags
- [Content and front matter](/docs/content-and-frontmatter/) — front matter properties

