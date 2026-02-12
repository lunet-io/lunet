# Extends Plugin

The `extend` function loads reusable Lunet content layers.

This plugin no longer uses a central registry. You point directly to a GitHub repository or a local extension folder.

## Quick Start

```scriban
# Latest `main` from GitHub (expects files under `dist/`)
extend "lunet-io/lunet-extends-jquery"

# Specific tag
extend "lunet-io/lunet-extends-jquery@v1.2.3"
```

## Supported Inputs

`extend` accepts either:

- A string
- An object

### String Syntax

```scriban
# GitHub owner/repo (latest main)
extend "owner/repo"

# GitHub owner/repo with tag
extend "owner/repo@tag"

# GitHub URL (latest main)
extend "https://github.com/owner/repo"

# GitHub URL with tag
extend "https://github.com/owner/repo@tag"

# Local extension from /.lunet/extends/<name>
extend "my-local-extension"
```

### Object Syntax

```scriban
extend {
  repo: "owner/repo",     # or url: "https://github.com/owner/repo"
  tag: "v1.2.3",          # optional (default: latest main)
  directory: "dist",      # optional (default: dist)
  public: true,           # optional (default: false/private cache)
  name: "my-extension"    # optional display name
}
```

## Content Convention

For GitHub extensions, Lunet downloads a repository ZIP and extracts only one directory:

- Default: `dist`
- Overridable via `directory` in object syntax

A typical repository layout is:

```text
README.md
dist/
  ... content files ...
  .lunet/
    ... metadata files (layouts/includes/data/...)
```

The extracted `dist/.lunet` becomes part of the extension metadata layer automatically.

## Caching and Install Location

- Default (`public` not set, or `public: false`): installs to cache under `/.lunet/build/cache/.lunet/extends/...`
- `public: true`: installs under `/.lunet/extends/...`

GitHub extensions without an explicit tag use `main` and are refreshed once per Lunet process.

## Loading Behavior

When an extension is loaded, Lunet:

1. Adds the extension filesystem as a content layer
2. Imports `config.scriban` from the extension root if present

## Migration From Registry-Based Extends

Old registry names like `extend "jquery"` no longer resolve remotely by default.

Use one of:

```scriban
extend "owner/repo"
# or
extend { repo: "owner/repo", tag: "v1.2.3" }
```
