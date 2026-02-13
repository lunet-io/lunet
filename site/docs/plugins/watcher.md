---
title: "Watcher module (--watch)"
---

# Watcher module (`--watch`)

Lunet can watch for changes and rebuild incrementally.

## Build with watch

```shell-session
lunet build --watch
```

## Serve with watch

```shell-session
lunet serve
```

Notes:
- output changes under `.lunet/build/**` are ignored by the watcher
- `--no-threads` is useful for debugging complex build behavior

