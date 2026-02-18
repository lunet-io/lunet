---
title: "Watcher module (--watch)"
---

# Watcher module (`--watch`)

The watcher module monitors your site files for changes and triggers a rebuild automatically. It also registers the `build` CLI command.

## Build with watch

```shell-session
lunet build --watch
```

After the initial build, the watcher monitors for file changes and rebuilds the entire site when changes are detected.

## Build CLI options

{.table}
| Option | Default | Description |
|---|---|---|
| `--watch` | disabled | Enable file watching after build |
| `--dev` | disabled | Set environment to `"dev"` (production otherwise) |
| `--no-threads` | disabled | Disable multi-threading (useful for debugging) |

## Serve with watch

```shell-session
lunet serve
```

The [Server module](server.md) enables `--watch` and `--dev` by default.

## How watching works

The watcher creates file system watchers for each directory in your site. When files change:

1. Events are collected and **debounced** — the watcher waits 200ms after the last change before triggering a rebuild.
2. Duplicate events for the same file are squashed (only the latest event is kept).
3. Events in excluded paths are filtered out.
4. A full site rebuild is triggered with the remaining changes.

## Excluded paths

The following paths are always excluded from watching:

- `.lunet/build/**` — the output directory
- Directories starting with `"new"` under `.lunet/`

The site configuration file is **always** watched, regardless of other exclusion rules.

## See also

- [Server module](server.md) — `lunet serve` with live reload
- [CLI reference](/docs/cli/) — all command-line options

