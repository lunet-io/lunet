---
title: "TOML module"
---

# TOML module

The TOML module registers a data loader for `.toml` files in `/.lunet/data/`. It depends on the [Datas module](datas.md) and uses the [Tomlyn](https://github.com/xoofx/tomlyn) library for parsing.

## Example

Create a data file at `/.lunet/data/project.toml`:

```toml
name = "Lunet"
version = "1.0"

[database]
server = "192.168.1.1"
ports = [8001, 8001, 8002]
```

Access it in templates:

```scriban
site.data.project.name
site.data.project.database.server
```

## Type conversion

TOML values are converted to Scriban-compatible types:

{.table}
| TOML type | Scriban type | Notes |
|---|---|---|
| Table | ScriptObject | Nested property access |
| Array | ScriptArray | Index and iteration |
| Array of tables | ScriptArray | Each element is a ScriptObject |
| String | string | |
| Integer | int or long | |
| Float | float or double | |
| Boolean | bool | |
| Date-time | DateTime | Offset information may be lost |

## See also

- [Datas module](datas.md) — the `site.data` container
- [JSON module](json.md) — alternative data format
- [YAML module](yaml.md) — alternative data format with front matter support
