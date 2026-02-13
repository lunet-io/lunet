---
title: "TOML module"
---

# TOML module

The TOML module registers a data loader for `/.lunet/data/**/*.toml`.

Example:

```toml
name = "Lunet"
```

Use it via `site.data`:

```scriban
{{ '{{ site.data.project.name }}' }}
```
