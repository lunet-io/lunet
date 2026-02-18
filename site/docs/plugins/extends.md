---
title: "Extends module (themes)"
---

# Extends module (themes)

The extends module loads themes and extensions into your site. Extensions provide layouts, includes, styles, scripts, and configuration that layer underneath your site's own files.

The `extend` function is called in `config.scriban`:

```scriban
extend "owner/repo@1.0.0"
```

For a detailed guide on how themes work, how the layered filesystem operates, and how to override theme files, see [Themes & extensions](../themes-and-extends.md).

## GitHub extensions

### Latest main branch

```scriban
extend "owner/repo"
```

Downloads the `dist/` directory from the `main` branch of `https://github.com/owner/repo`. On each build, the latest version is re-downloaded once per session.

### Pinned to a tag

```scriban
extend "owner/repo@1.0.0"
```

Downloads the `dist/` directory from the specified Git tag. Pinned versions are downloaded once and cached.

### Full URL

```scriban
extend "https://github.com/owner/repo"
```

GitHub URLs are also accepted. The `.git` suffix is stripped automatically.

## Local extensions

To use a local theme during development, place it under:

```text
<your-site>/.lunet/extends/<name>/
```

Then load it by name (no `/` in the name):

```scriban
extend "mytheme"
```

When the name does not contain a `/`, Lunet looks for it locally instead of on GitHub.

## Object syntax

For full control, pass an object:

```scriban
extend {
  repo: "owner/repo",
  tag: "1.0.0",
  directory: "dist",
  public: true
}
```

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `repo` (or `url`) | string | (required, or `name`) | GitHub repository (`"owner/repo"`) or full GitHub URL |
| `name` | string | derived from repo | Short name for the extension |
| `tag` (or `version`) | string | `null` (latest `main`) | Git tag to pin |
| `directory` | string | `"dist"` | Subdirectory in the repository to extract |
| `public` | bool | `false` | Store in `.lunet/extends/` (committable) instead of global cache |

## How extensions layer

When you call `extend`, the extension's file system is added to the site's aggregate file system. Your site's own files **always take priority** over extension files. This means you can override any theme file by placing a file at the same path in your site.

Multiple extensions layer in the order they are loaded — later `extend` calls have higher priority than earlier ones.

## Extension `config.scriban`

If the extension's root contains a `config.scriban`, it is automatically imported into the site's scripting context. This allows extensions to:

- Set default variables and configuration
- Define Scriban functions
- Register bundles, include paths, or other settings
- Call `extend` themselves (transitive dependencies)

## Convention: `dist/` folder structure

Lunet expects theme content to live under `dist/` in the repository:

```text
repo/
  readme.md
  dist/
    config.scriban          (optional — runs during site config)
    readme.md               (optional — default home page)
    .lunet/
      layouts/              (layout templates)
      includes/             (include templates)
      css/                  (stylesheets)
      js/                   (scripts)
```

Everything under `dist/` becomes available as if it were part of your site, but at a lower priority than your own files.

## Caching

- **Tagged extensions** are downloaded once and cached indefinitely.
- **Untagged extensions** (latest `main`) are re-downloaded once per build session to check for updates.
- **Private caching** (default): stored in the build cache directory, not committed to version control.
- **Public caching** (`public: true`): stored in `.lunet/extends/`, suitable for committing to version control.

## Error handling

- If a local extension name is not found under `.lunet/extends/`, the build logs an error suggesting the GitHub `"owner/repo"` syntax.
- If a GitHub download fails (network error, missing repository), the build logs an error with the URL and reason.
- If the specified `directory` does not exist in the downloaded archive, the build logs an error.
- Loading the same extension twice is safe — it returns the cached instance without re-downloading.

## See also

- [Themes & extensions guide](../themes-and-extends.md) — in-depth guide on theming
- [Bundles module](bundles.md) — extensions often define default bundles
- [Resources module](resources.md) — downloading npm packages (separate from extends)
