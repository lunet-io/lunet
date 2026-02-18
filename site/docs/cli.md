---
title: "CLI reference"
---

# CLI reference

Run Lunet from a site folder (the folder that contains `config.scriban`).

## Global options

These options apply to all commands:

{.table}
| Option | Description |
|---|---|
| `-d, --define <name=value>` | Define a site variable (executed as a Scriban statement). Can be repeated. |
| `-o, --output-dir <path>` | Override the output directory (default: `.lunet/build/www/`) |
| `-i, --input-dir <path>` | Override the input directory (default: `.`, current directory) |
| `--stacktrace` | Show full stack traces when errors occur (useful for debugging) |
| `-h, --help` | Show help |
| `-v, --version` | Show Lunet version |

### How `--define` works

Each `--define` value is executed as a **Scriban statement** against the `SiteObject`. Because the scripting context is the site itself, `--define "baseurl=https://staging.example.com"` sets `site.baseurl`, exactly as if you had written `baseurl = "https://staging.example.com"` in `config.scriban`.

You can use any valid Scriban expression:

```shell-session
lunet build --define "baseurl=https://staging.example.com"
lunet build -d "minify=false" -d "environment='staging'"
```

Defines are evaluated **before** `config.scriban` runs. This lets you use `??` (null-coalescing) in config to provide overridable defaults:

```scriban
baseurl = baseurl ?? "https://example.com"
```

## Commands

### `lunet init [folder]`

Creates a new site folder by copying the built-in skeleton template.

```shell-session
lunet init mysite
```

The skeleton includes a minimal `config.scriban`, default layouts, and a sample home page. If `[folder]` is omitted, the current directory is used.

Options:

{.table}
| Option | Description |
|---|---|
| `-f, --force` | Create even if the target folder is not empty |

### `lunet clean`

Deletes the `.lunet/build` folder in the current site, removing all generated output and caches.

```shell-session
lunet clean
```

Requires `config.scriban` to exist in the current (or `--input-dir`) directory, otherwise prints an error.

### `lunet build`

Builds the site once (or continuously with `--watch`).

```shell-session
lunet build
lunet build --dev
lunet build --watch
lunet build --define "baseurl=https://staging.example.com"
```

Options:

{.table}
| Option | Description |
|---|---|
| `--watch` | Watch for file changes and rebuild automatically (see [Watcher module](plugins/watcher.md)) |
| `--dev` | Set `site.environment = "dev"` (default is `"prod"`) |
| `--no-threads` | Disable multi-threaded processing (useful for debugging) |

When `--dev` is set, modules like [Tracking](plugins/tracking.md) disable production-only behavior (e.g. analytics injection).

### `lunet serve`

Builds the site, starts a local web server, and watches for changes.

```shell-session
lunet serve
```

Options:

{.table}
| Option | Description |
|---|---|
| `--no-watch` | Disable file watching (serve only, no rebuild on changes) |
| `--no-threads` | Disable multi-threaded processing |

`serve` always operates in development mode (`site.environment = "dev"`); there is no `--dev` flag because it is enabled by default.

Notes:

- Base URL is overridden to `http://localhost:4000` unless `baseurlforce = true` is set in `config.scriban`. There is no CLI option to change the port; set `baseurl` in config if you need a different port.
- Live reload is enabled by default — a small script is injected into `<head>` that connects via WebSocket and triggers a browser refresh on rebuild. See [Server module](plugins/server.md).
- Error responses redirect to `/404.html` if the file exists.

## See also

- [Configuration (`config.scriban`)](configuration.md) — site variables, `--define` override patterns
- [Server module](plugins/server.md) — live reload, base URL override, error handling
- [Watcher module](plugins/watcher.md) — file watching, debounce, rebuild behavior
- [Getting started](getting-started.md) — first steps with `lunet init`, `build`, and `serve`
