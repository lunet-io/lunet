---
title: "Configuration (config.scriban)"
---

# Configuration (`config.scriban`)

Unlike most static site generators, Lunet’s configuration file is **Scriban code**.

That means you can:
- compute values dynamically,
- conditionally enable features,
- download resources or themes,
- build up bundles/taxonomies programmatically.

## Minimal config

```scriban
title = "My site"
baseurl = baseurl ?? "https://example.com"
basepath = "/"
```

`baseurl` can be provided externally (for example by CI) and will be overridden by `lunet serve` unless you force it.

To prevent `lunet serve` from overriding `baseurl` and `basepath`:

```scriban
baseurlforce = true
```

## Common site variables

- `site.title` — site title (used by layouts and RSS)
- `site.description` — site description (used by cards/RSS)
- `site.baseurl` — canonical URL (used to generate absolute links)
- `site.basepath` — URL prefix when hosted under a sub-path (GitHub Pages)
- `site.environment` — `"prod"` or `"dev"` (set by CLI)
- `site.layout` — default layout name (fallback is `_default`)

Other common switches:

- `site.url_as_file` — when `true`, HTML pages keep their filename in the URL (`/docs/intro.html`).
- `site.readme_as_index` — when `true`, `readme.md` behaves like `index.md` for folder URLs.
- `site.default_page_ext` — default output extension for HTML (must be `.html` or `.htm`).

## Includes and excludes

Lunet decides whether a file is handled using three glob collections:

- `site.force_excludes` — cannot be overridden (e.g. `**/.lunet/build/**`)
- `site.includes` — overrides `excludes`
- `site.excludes` — ignored when a file matches `includes`

You can customize them in config:

```scriban
excludes.add "**/*.psd"
includes.add "**/.lunet/**" # already included by default
```

## Logging from config

Use the built-in `log` object:

```scriban
log.info "Config loaded"
log.warn "Something looks off"
```

Control verbosity:

```scriban
builtins.log.level = "debug" # trace|debug|info|warn|error|fatal
```

## Using modules from config

Many modules expose a root object (for example `bundle`, `resources`, `scss`, `taxonomies`, `search`).

Example:

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  concat = true
  minify = true
end
```
