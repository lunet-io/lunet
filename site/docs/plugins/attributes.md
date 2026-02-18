---
title: "Attributes module (URL patterns)"
---

# Attributes module (URL patterns)

The attributes module lets you apply metadata to files by glob pattern **before** they are loaded. Matched setters act as defaults — front matter values in the file always take precedence.

The most common use is URL rewriting for blog-style dated posts, but any front-matter key can be set (e.g. `layout`, `draft`, custom metadata).

## Functions

> All `attributes.*` functions shown on this page are called inside your site's `config.scriban`, within a `with attributes` / `end` block.

{.table}
| Function | Description |
|---|---|
| `attributes.match(pattern, setters)` | Apply `setters` to files whose path **matches** the glob pattern |
| `attributes.unmatch(pattern, setters)` | Apply `setters` to files whose path does **not match** the glob pattern |
| `attributes.clear` | Remove all rules, including the built-in default |

## Example: blog-style URLs

```scriban
with attributes
  match "/blog/**/[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]*.md" {
    url: "/blog/:year/:month/:day/:slug:output_ext"
  }
end
```

## Setting arbitrary metadata

Setters are not limited to `url`. Any key/value pair can be applied:

```scriban
with attributes
  match "/drafts/**" { draft: true, layout: "draft" }
  match "/blog/**" { layout: "post", section: "blog" }
end
```

## Rule evaluation

- Rules are evaluated **in order** (first-added, first-evaluated).
- **All** matching rules apply — they do not short-circuit.
- When multiple rules match the same file, later rules overwrite earlier ones for the same key.
- Setters are applied as defaults before front matter is parsed, so front matter values always win.

## URL placeholders

Supported placeholders in `url` patterns:

{.table}
| Placeholder | Format | Description |
|---|---|---|
| `:year` | `yyyy` | Four-digit year |
| `:short_year` | `yy` | Two-digit year |
| `:month` | `MM` | Zero-padded month |
| `:i_month` | `M` | Month without padding |
| `:short_month` | `MMM` | Abbreviated month name (e.g. "Jan") |
| `:long_month` | `MMMM` | Full month name (e.g. "January") |
| `:day` | `dd` | Zero-padded day |
| `:i_day` | `d` | Day without padding |
| `:y_day` | `000` | Day of year (e.g. "045") |
| `:week` | `00` | Calendar week number (first-four-day-week, Monday start) |
| `:w_year` | `00` | ISO week number |
| `:w_day` | `1`–`7` | Day of week (Monday=1, Sunday=7) |
| `:short_day` | `ddd` | Abbreviated day name (e.g. "Mon") |
| `:long_day` | `dddd` | Full day name (e.g. "Monday") |
| `:hour` | `HH` | 24-hour hour |
| `:minute` | `mm` | Zero-padded minute |
| `:second` | `ss` | Zero-padded second |
| `:title` | | Handleized page title |
| `:slug` | | Page slug |
| `:section` | | First directory segment of the path |
| `:slugified_section` | | Handleized section name |
| `:output_ext` | | Output file extension (e.g. `.html`) |
| `:path` | | Full input file path |

If a placeholder resolves to an empty value, it and its preceding `/` are removed from the URL. Unknown placeholders produce a warning and are removed.

## Built-in default

Lunet ships with a built-in rule that gives dated files a blog-style URL:

- **Pattern:** `/**/[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]*.*`
- **URL:** `/:section/:year/:month/:day/:slug:output_ext`

This matches any file whose name starts with an 8-digit date pattern (e.g. `2024-01-15-my-post.md`).

To remove this default, call `attributes.clear` before defining your own rules:

```scriban
with attributes
  attributes.clear
  match "/posts/**/*.md" { url: "/:slug/" }
end
```

## See also

- [Content and front matter](/docs/content-and-frontmatter/) — front matter overrides attribute setters
- [Site structure](/docs/site-structure/) — how file paths relate to URLs
