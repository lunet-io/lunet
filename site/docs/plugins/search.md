---
title: "Search module"
---

# Search module

The search module generates a full-text search database from your site content and injects the necessary runtime assets into your bundle for client-side search.

## Enable search

```scriban
with search
  enable = true
end
```

## How it works

During the build, Lunet:

1. Scans all pages (excluding those matching `search.excludes`).
2. Extracts searchable text (title and body content).
3. Builds an SQLite FTS (full-text search) database.
4. Writes the database to the output (default: `/js/lunet-search.db`).
5. Injects a WASM-based SQLite runtime and search UI into your bundle.

At runtime, the search database is loaded in the browser and queries run entirely client-side — no server required.

## Configuration

```scriban
with search
  enable = true
  url = "/js/lunet-search.db"   # output path for the search database
  worker = true                  # use a web worker for search (default: true)
end
```

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `search.enable` | bool | `false` | Enable search index generation |
| `search.url` | string | `"/js/lunet-search.db"` | Output URL for the search database |
| `search.worker` | bool | `true` | Run search in a web worker for better performance |

## Excluding content from search

Use glob patterns to exclude pages from the search index:

```scriban
with search
  excludes.add ["/api/**", "/draft/**"]
end
```

You can also exclude individual pages via front matter by setting `discard: true` (which excludes them from all output, not just search).
