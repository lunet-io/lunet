---
title: "JSON module"
---

# JSON module

The JSON module registers a data loader for `/.lunet/data/**/*.json`.

Example:

```json
{
  "name": "Lunet"
}
```

Use it via `site.data`:

```scriban
{{ '{{ site.data.project.name }}' }}
```
