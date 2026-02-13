---
title: "Server module (lunet serve)"
---

# Server module (`lunet serve`)

`lunet serve` runs a local web server and optionally live reload.

## Live reload

Live reload is enabled by default and injects a small client snippet into the `<head>`.

You can disable it in config:

```scriban
site.livereload = false
```

## Server logging

Server logging is disabled by default. Enable it via:

```scriban
builtins.log.server = true
```

