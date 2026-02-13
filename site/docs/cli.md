---
title: "CLI reference"
---

# CLI reference

Run Lunet from a site folder (the folder that contains `config.scriban`).

## `lunet init [folder]`

Creates a new site folder (skeleton) by copying the built-in template.

```shell-session
lunet init mysite
```

## `lunet clean`

Deletes `.lunet/build` in the current site folder.

```shell-session
lunet clean
```

## `lunet build`

Builds the site once.

Options:
- `--watch` — rebuild on file changes
- `--dev` — use development environment (`site.environment = "dev"`)
- `--no-threads` — single-threaded processing (useful for debugging)

Examples:

```shell-session
lunet build
lunet build --dev
lunet build --watch
```

## `lunet serve`

Builds the site, starts a local web server, and optionally watches for changes.

Options:
- `--no-watch` — serve without watching/rebuild
- `--no-threads` — single-threaded processing

Notes:
- Base URL and base path are overridden for local serving unless you force them in config.
- Live reload is enabled by default.

```shell-session
lunet serve
```

