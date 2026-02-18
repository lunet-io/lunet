---
title: "YAML module"
---

# YAML module

The YAML module provides two features:

1. A **data loader** for `.yml` and `.yaml` files in `/.lunet/data/`
2. A **front matter parser** for `---`-delimited YAML front matter in content pages

It depends on the [Datas module](datas.md) and uses the [SharpYaml](https://github.com/xoofx/SharpYaml) library with the extended schema.

## YAML front matter

Pages can include YAML front matter delimited by `---`:

```markdown
---
title: "Hello"
tags: ["docs", "guide"]
date: 2024-01-15
---

# Hello
This is content.
```

Front matter must start with an explicit `---` delimiter. All key-value pairs become page-level template variables accessible in layouts and includes.

## YAML data files

Place data files under `/.lunet/data/`:

```yaml
# /.lunet/data/project.yml
name: Lunet
version: "1.0"
```

Access it in templates:

```scriban
site.data.project.name
```

## Type conversion

YAML scalar values are automatically typed using SharpYaml's extended schema:

{.table}
| YAML value | Scriban type | Examples |
|---|---|---|
| Mapping | ScriptObject | `key: value` |
| Sequence | ScriptArray | `[a, b, c]` or `- item` |
| Integer | int or long | `42`, `0xFF` |
| Float | double | `3.14`, `.inf` |
| Boolean | bool | `true`, `false`, `yes`, `no` |
| Null | null | `null`, `~` |
| Timestamp | DateTime | `2024-01-15` |
| String | string | `"hello"`, unquoted text |

## Multi-document YAML

Data files with multiple `---`-separated documents produce a ScriptArray of top-level objects:

```yaml
---
name: "First"
---
name: "Second"
```

This becomes a ScriptArray with two ScriptObject elements.

## Edge cases

- Front matter must begin with explicit `---` — files without this delimiter are treated as having no front matter.
- Duplicate keys in a YAML mapping cause an error.
- Front matter values can be overwritten by later script execution (they are not read-only).

## See also

- [Datas module](datas.md) — the `site.data` container
- [Content and front matter](/docs/content-and-frontmatter/) — how front matter works
- [JSON module](json.md) — alternative data format
- [TOML module](toml.md) — alternative data format
