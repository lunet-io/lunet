---
title: "JSON module"
---

# JSON module

The JSON module registers a data loader for `.json` files in `/.lunet/data/`. It depends on the [Datas module](datas.md).

## Example

Create a data file at `/.lunet/data/project.json`:

```json
{
  "name": "Lunet",
  "version": "1.0"
}
```

Access it in templates:

```scriban
site.data.project.name
```

## Type conversion

JSON values are converted to Scriban-compatible types:

{.table}
| JSON type | Scriban type | Notes |
|---|---|---|
| Object | ScriptObject | Nested property access |
| Array | ScriptArray | Index and iteration |
| String | string | |
| Number | int, long, decimal, or double | Smallest fitting type is used |
| Boolean | bool | |
| null | null | |

## Parsing behavior

- **Trailing commas** are allowed (non-standard JSON extension).
- **Comments** (`//` and `/* */`) are allowed and skipped during parsing.
- Parse errors are reported and the file is skipped.

## See also

- [Datas module](datas.md) — the `site.data` container
- [YAML module](yaml.md) — alternative data format with front matter support
- [TOML module](toml.md) — alternative data format
