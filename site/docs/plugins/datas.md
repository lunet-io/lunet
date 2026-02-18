---
title: "Datas module"
---

# Datas module

The datas module provides the `site.data` object — a tree of data loaded from files in `/.lunet/data/`. Format-specific modules register their loaders into this plugin:

- [YAML](yaml.md) — loads `.yml` and `.yaml` files
- [JSON](json.md) — loads `.json` files
- [TOML](toml.md) — loads `.toml` files

## How it works

All data files from `/.lunet/data/` are loaded **before** content processing begins, so data is available in `config.scriban`, layouts, includes, and page templates.

Data files are keyed by **filename without extension**:

```plaintext
/.lunet/data/
  project.yml      → site.data.project
  authors.json     → site.data.authors
  settings.toml    → site.data.settings
  blog/
    tags.yml       → site.data.blog.tags
```

Subdirectories create nested objects automatically.

## Accessing data

```scriban
site.data.project.name
site.data.authors[0].email
```

## Edge cases

- If two files in the same directory resolve to the same key (e.g. `product.json` and `product.yml`), only the first one loaded is kept and a warning is logged.
- Files without a file extension are silently skipped.
- If a data file fails to parse, an error is logged and processing continues.
- `site.data` is read-only at the top level — you cannot replace it from Scriban, but you can read all its children.

## See also

- [YAML module](yaml.md) — `.yml` / `.yaml` data loader and front matter parser
- [JSON module](json.md) — `.json` data loader
- [TOML module](toml.md) — `.toml` data loader

