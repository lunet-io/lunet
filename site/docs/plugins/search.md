---
title: "Search module"
---

# Search module

The search module generates a search database and injects the necessary runtime assets into your bundle.

## Enable search

```scriban
with search
  enable = true
  engine = "sqlite" # sqlite|lunr
  worker = true
  url = "/js/lunet-search.db"
end
```

## Engines

- `sqlite` (default) produces an SQLite FTS database and ships a WASM runtime for client-side search.
- `lunr` produces a JSON index and uses the `lunr` npm package.

## Excluding content

```scriban
search.excludes.add ["/banner.md", "/docs/todos/**"]
```

