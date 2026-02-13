---
title: "Extends module (themes)"
---

# Extends module (themes)

The extends module implements the global `extend` function used from `config.scriban`.

From a user perspective, an “extension” is usually a **theme repository** that Lunet downloads and layers on top of your site.

## Supported forms

### GitHub repository (latest `main`)

```scriban
extend "owner/repo"
```

### GitHub repository pinned to a tag

```scriban
extend "owner/repo@1.0.0"
```

### Object syntax

```scriban
extend {
  repo: "owner/repo",
  tag: "1.0.0",
  directory: "dist",
  public: true
}
```

Notes:
- `directory` defaults to `dist`.
- If the extension contains `dist/config.scriban`, it is imported automatically.

## Local extensions

To use a local theme, place it under your site:

`/.lunet/extends/<name>/`

Then:

```scriban
extend "<name>"
```

## Convention: `dist/` + `.lunet/`

Lunet expects theme content to live under `dist/`:

```text
repo/
  readme.md
  dist/
    config.scriban          (optional)
    readme.md               (optional home page)
    .lunet/
      layouts/
      includes/
      data/
```

Everything under `dist/` becomes available as if it were part of your site.
