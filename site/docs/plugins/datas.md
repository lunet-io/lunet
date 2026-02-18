---
title: "Datas module"
---

# Datas module

The datas module owns the `site.data` root object. It is the container that format-specific modules register their loaders into:

- [YAML](yaml.md) — loads `.yml` and `.yaml` files
- [JSON](json.md) — loads `.json` files
- [TOML](toml.md) — loads `.toml` files

All data files from `/.lunet/data/**` are loaded **before** content processing begins, so data is available in `config.scriban`, layouts, includes, and page templates via `site.data.<filename>`.

For usage examples, see the [Data modules](data.md) page.

