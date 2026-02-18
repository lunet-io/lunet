---
title: "Data modules (site.data)"
---

# Data modules (`site.data`)

Data files under `/.lunet/data/` are loaded into `site.data` before content processing begins, making them available in `config.scriban`, layouts, includes, and page templates.

## Supported formats

{.table}
| Format | Extension | Module |
|---|---|---|
| YAML | `.yml`, `.yaml` | [YAML module](yaml.md) |
| JSON | `.json` | [JSON module](json.md) |
| TOML | `.toml` | [TOML module](toml.md) |

## Example

Create `/.lunet/data/project.yml`:

```yaml
name: Lunet
url: "https://github.com/xoofx/lunet"
stars: 123
```

Use it in a page or layout:

```scriban
site.data.project.name
site.data.project.stars
```

Subdirectories create nested objects:

```plaintext
/.lunet/data/
  project.yml      → site.data.project
  blog/
    tags.yml       → site.data.blog.tags
```

## See also

- [Datas module](datas.md) — the `site.data` container and loading mechanism
- [YAML module](yaml.md) — also provides front matter parsing for content pages
