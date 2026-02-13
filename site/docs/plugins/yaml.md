---
title: "YAML module"
---

# YAML module

The YAML module provides:
- a data loader for `/.lunet/data/**/*.yml` / `*.yaml`
- YAML front matter parsing (`--- ... ---`) for pages

## YAML front matter

```markdown
---
title: "Hello"
tags: ["docs"]
---

# Hello
```

## YAML data files

Put data under `/.lunet/data/`:

```yaml
# /.lunet/data/project.yml
name: Lunet
```

Then access it via `site.data`:

```scriban
{{ '{{ site.data.project.name }}' }}
```
