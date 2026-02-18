---
title: "Extends module (themes)"
---

# Extends module (themes)

The extends module implements the global `extend` function used from `config.scriban` to load themes and extensions.

For a detailed guide on how themes work, how the layered filesystem operates, and how to override theme files, see [Themes & extensions](../themes-and-extends.md).

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

- `directory` defaults to `dist`.
- If the extension contains `dist/config.scriban`, it is imported automatically into the current site context.

## Local extensions

To use a local theme during development, place it under:

```text
<your-site>/.lunet/extends/<name>/
```

Then load it by name:

```scriban
extend "mytheme"
```

When the name does not contain a `/`, Lunet looks for it locally instead of on GitHub.

## Convention: `dist/` folder structure

Lunet expects theme content to live under `dist/`:

```text
repo/
  readme.md
  dist/
    config.scriban          (optional — runs during site config)
    readme.md               (optional — default home page)
    .lunet/
      layouts/              (layout templates)
      includes/             (include templates)
      data/                 (data files)
```

Everything under `dist/` becomes available as if it were part of your site, but at a lower priority than your own files. See [Site structure](../site-structure.md) for how the layered filesystem works.
