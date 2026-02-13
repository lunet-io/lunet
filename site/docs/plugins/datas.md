---
title: "Datas module"
---

# Datas module

The datas module owns the `site.data` root object.

Format-specific modules register loaders into it:
- [YAML](yaml.md)
- [JSON](json.md)
- [TOML](toml.md)

All data is loaded from `/.lunet/data/**` before content processing so it is available in layouts and includes.

