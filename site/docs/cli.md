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

## Commands

### `lunet init [folder]`

Creates a new site folder by copying the built-in skeleton template.

```shell-session
lunet init mysite
```

Options:

- `-f, --force` — create even if the target folder is not empty

### `lunet clean`

Deletes the `.lunet/build` folder in the current site, removing all generated output and caches.

```shell-session
lunet clean
```

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
| `--watch` | Watch for file changes and rebuild automatically |
| `--dev` | Set `site.environment = "dev"` (default is `"prod"`) |
| `--no-threads` | Disable multi-threaded processing (useful for debugging) |

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

Notes:

- Base URL is overridden to `http://localhost:4000` unless `baseurlforce = true` is set in config.
- `site.environment` is set to `"dev"`.
- Live reload is enabled by default (injects a small script into `<head>`).

### `lunet config`

Displays the current site configuration variables. Useful for debugging config issues.

```shell-session
lunet config
```
