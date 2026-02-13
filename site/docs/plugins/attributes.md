---
title: "Attributes module (URL patterns)"
---

# Attributes module (URL patterns)

The attributes module lets you apply metadata to files by glob pattern before they are loaded.

The most common use is URL rewriting.

## Example: blog-style URLs

```scriban
with attributes
  match "/blog/**/[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]*.md" {
    url: "/blog/:year/:month/:day/:slug:output_ext"
  }
end
```

## URL placeholders

Supported placeholders in `url` patterns:

{.table}
| Placeholder | Description |
|---|---|
| `:year`, `:short_year` | `yyyy` / `yy` |
| `:month`, `:i_month`, `:short_month`, `:long_month` | Month forms |
| `:day`, `:i_day`, `:y_day` | Day forms |
| `:week`, `:w_year`, `:w_day` | Week and weekday forms |
| `:hour`, `:minute`, `:second` | Time forms |
| `:title` | Handleized title |
| `:slug` | Slug |
| `:section`, `:slugified_section` | Section folder |
| `:output_ext` | File extension (e.g. `.html`) |
| `:path` | Full input path |

## Built-in defaults

Lunet ships with a default matcher that applies a blog-like URL pattern for dated files:

`/**/YYYY-MM-DD-*.ext`
