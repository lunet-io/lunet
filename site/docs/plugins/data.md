---
title: "Data modules (site.data)"
---

# Data modules (`site.data`)

Data files under `/.lunet/data/` are loaded into `site.data`.

Supported formats depend on enabled loaders:
- YAML (`.yml`, `.yaml`)
- JSON (`.json`)
- TOML (`.toml`)

## Example

Create `/.lunet/data/project.yml`:

```yaml
name: Lunet
stars: 123
```

Use it in a page:

```scriban
{{ '{{ site.data.project.name }}' }}
```
