---
title: "Server module (lunet serve)"
---

# Server module (`lunet serve`)

The server module runs a local development web server with live reload support. It serves your built site on `http://localhost:4000` and automatically rebuilds when files change.

## Usage

```shell-session
lunet serve
```

The serve command always runs in `dev` environment, so tracking scripts and other production-only features are disabled.

### CLI options

{.table}
| Option | Default | Description |
|---|---|---|
| `--no-watch` | watch enabled | Disable file watching (no auto-rebuild) |
| `--no-threads` | threading enabled | Disable multi-threading (useful for debugging) |

## Live reload

Live reload is **enabled by default**. When a rebuild completes, connected browsers automatically refresh the page via a WebSocket connection.

The module injects a small `<script>` snippet into every page's `<head>` that connects to the server's `/__livereload__` WebSocket endpoint.

To disable live reload:

```scriban
site.livereload = false
```

## Base URL handling

During `lunet serve`, the base URL is automatically overridden to `http://localhost:4000` and the base path is set to `""`. This ensures local links work correctly regardless of your production `baseurl` setting.

To preserve your configured `baseurl` and `basepath` during serve (e.g. for testing path-based deployments):

```scriban
site.baseurlforce = true
```

## Error pages

The server uses `site.error_redirect` (default: `"/404.html"`) for status code error pages. If you have a custom 404 page, it will be served automatically for missing URLs.

## Server logging

Server request logging is **enabled by default** during `lunet serve`:

```scriban
builtins.log.server = true
```

Set to `false` to suppress request logs.

## Response compression

The server automatically compresses responses for better performance during development.

## See also

- [Watcher module](watcher.md) — file watching and rebuild behavior used by `serve`
- [Configuration](/docs/configuration/) — `site.baseurl` and environment settings
- [Tracking module](tracking.md) — disabled during `dev` environment

