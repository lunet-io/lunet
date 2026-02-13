---
title: "Site structure"
---

# Site structure

Lunet builds a site from your **input** files plus a layered **meta** folder named `/.lunet/`.

## Typical layout

```text
mysite/
  config.scriban
  readme.md
  docs/
    getting-started.md
  css/
    main.scss
  js/
    main.js
  .lunet/
    includes/
    layouts/
    data/
    extends/
    build/
      www/          (generated output)
      cache/        (cache for resources/extensions)
```

## The `/.lunet/` folder

`/.lunet/` is a convention used by Lunet plugins:

- `/.lunet/includes/**` — Scriban `include` templates.
- `/.lunet/layouts/**` — layout templates.
- `/.lunet/data/**` — data files loaded into `site.data` (YAML/JSON/TOML).
- `/.lunet/extends/**` — local extensions/themes (optional).
- `/.lunet/build/**` — build output and caches (generated).

## Output folder

By default, Lunet writes the generated site to:

`/.lunet/build/www/`

Static assets are copied as-is unless a plugin converts them (for example SCSS → CSS).

