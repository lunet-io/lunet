---
title: "Search module"
---

# Search module

The search module generates a full-text search database from your site content and injects the necessary runtime assets into your bundle for client-side search. Queries run entirely in the browser — no server required.

## Enable search

Search is **disabled** by default. Enable it in your `config.scriban`:

```scriban
with search
  enable = true
end
```

> All `search.*` properties shown on this page are set inside your site's `config.scriban`, within a `with search` / `end` block.

## How it works

During the build, Lunet:

1. Scans all HTML and Markdown pages — both static and dynamically generated (e.g. API pages).
2. Converts Markdown to HTML (via Markdig), then strips HTML to plain text (via NUglify).
3. Builds an **SQLite FTS5** full-text search database using the **porter** stemming tokenizer, so searches match word stems (e.g. "running" matches "run").
4. Optimizes and vacuums the database for minimal file size.
5. Writes the database to the output path (default: `/js/lunet-search.sqlite`).
6. Injects the WASM-based SQLite runtime and search engine JavaScript into your bundle.

At runtime, the search database is loaded in the browser and queries run client-side with **BM25 ranking**. Results include URL, title, and a highlighted content snippet.

> **No search UI is injected.** The plugin provides only the search engine and database. Your theme or layout must implement the search input and results display using the `LunetSearch` JavaScript API (see [Client-side API](#client-side-api) below).

## Configuration

```scriban
with search
  enable = true
  url = "/js/lunet-search.db"   # base URL for the search database
  worker = false                 # use a web worker for search (default: false)
  engine = "sqlite"              # search engine backend (default: "sqlite")
end
```

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `search.enable` | bool | `false` | Enable search index generation |
| `search.url` | string | `"/js/lunet-search.db"` | Base output URL for the search database. The actual file written uses a `.sqlite` extension |
| `search.worker` | bool | `false` | Run search in a web worker to avoid blocking the main thread |
| `search.engine` | string | `"sqlite"` | Search engine backend name. Currently only `"sqlite"` is built-in |
| `search.excludes` | list | `[]` | Glob patterns for pages to exclude from the search index |

> **Note on file extension:** Even though `url` defaults to `.db`, the output file is always written with a `.sqlite` extension (e.g. `/js/lunet-search.sqlite`). The client-side JavaScript also loads from the `.sqlite` path by default.

## Excluding content from search

Use glob patterns to exclude pages from the search index. Patterns must be **absolute paths**:

```scriban
with search
  excludes.add ["/api/**", "/draft/**"]
end
```

Pages with `discard: true` in front matter are excluded from all output, including search.

## Web worker mode

By default, the SQLite engine and search queries run on the main thread. Set `worker = true` to offload search to a [Web Worker](https://developer.mozilla.org/en-US/docs/Web/API/Web_Worker_API):

```scriban
with search
  enable = true
  worker = true
end
```

In worker mode, the search database is loaded asynchronously in a background thread. Queries are sent to the worker via `MessageChannel` and results are returned through the port. The trade-off is that `available` reports `true` immediately (before the worker finishes loading), so early queries may be delayed.

In non-worker mode (default), all assets are inlined into the bundle and the database is loaded on the main thread. The `available` property accurately reflects whether initialization is complete.

## Client-side caching

The search engine uses the browser [Cache API](https://developer.mozilla.org/en-US/docs/Web/API/Cache) (`caches.open("lunet.cache")`) to cache the database locally. On subsequent visits, a `HEAD` request checks `ETag`, `Last-Modified`, and `Content-Length` headers. The database is only re-downloaded when the server version has changed.

## Client-side API

The plugin exposes a global `DefaultLunetSearch` (or `DefaultLunetSearchPromise` in non-worker mode) object. After initialization, use the `query()` method:

```javascript
const search = await DefaultLunetSearchPromise;
const results = search.query("my search term");
// results: [{ url: "/page/", title: "Page Title", snippet: "...matched <b>text</b>..." }]
```

Each result contains:

{.table}
| Property | Type | Description |
|---|---|---|
| `url` | string | URL of the matching page |
| `title` | string | Page title |
| `snippet` | string | Content snippet with `<b>` highlighted matches |

Results are ranked by BM25 with default weights: URL (3.0), title (2.0), content (1.0). Snippet length defaults to 20 words. These weights are defined in the JavaScript client and are not configurable from Scriban.

> **FTS5 query syntax:** The search input supports [FTS5 query syntax](https://www.sqlite.org/fts5.html#full_text_query_syntax) including `AND`, `OR`, `NOT`, `*` (prefix), and `NEAR`. Malformed queries may cause errors.

## Error handling

- If `search.enable` is `false` (default), no processing occurs.
- If `search.engine` does not match any registered engine, an error is logged but the build continues.
- If `search.url` is not a valid absolute path, an error is logged and search is skipped.
- If engine initialization fails, all subsequent search processing is silently skipped.

## See also

- [Bundles module](bundles.md) — search assets are injected into the default bundle
- [Minifier module](minifier.md) — search JS can be minified as part of the bundle
- [SCSS module](scss.md) — for styling search UI components
