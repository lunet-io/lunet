---
title: "Configuration (config.scriban)"
---

# Configuration (`config.scriban`)

Unlike most static site generators, Lunet’s configuration file is **executable Scriban code**, not a passive data file like YAML or TOML.

This means you can:

- compute values dynamically,
- conditionally enable features,
- download resources or themes,
- build up bundles/taxonomies programmatically.

## How `config.scriban` works

Understanding the scripting context is the single most important concept in Lunet.

When Lunet loads `config.scriban`, the **scripting context is the `SiteObject` itself**. Every variable you assign in config is set directly on the site object. In other words:

```scriban
# These two lines are equivalent in config.scriban:
title = "My site"
site.title = "My site"
```

Because `config.scriban` runs “inside” the site, you can refer to any site property without a prefix. The `site.` prefix is optional but can be used for clarity.

> [!IMPORTANT]
>
> This is different from page templates, where you must use `site.title` to read the site title and `page.title` for the page title. See [Content & front matter](content-and-frontmatter.md) for the page context rules.

### Config vs page context (quick comparison)

{.table}
| Context | Scripting target | `title = "x"` sets… | Access site title as… | `include` allowed? |
|---|---|---|---|---|
| `config.scriban` | SiteObject (the site) | `site.title` | `title` or `site.title` | No |
| Page front matter | ContentObject (the page) | `page.title` | `site.title` | No |
| Page/layout body | — (both `site` and `page` in scope) | — (use `{{` `…` `}}`) | `site.title` | Yes (from `/.lunet/includes/`) |

### What runs when

`config.scriban` runs **once** at the start of every build, before any content is loaded or processed. This means:

1. You cannot access `site.pages` from config (pages haven’t been loaded yet).
2. Module configuration in config takes effect before the content pipeline runs.
3. Extensions loaded with `extend` are imported during config execution, so their `config.scriban` also runs at this stage.

## Minimal config

```scriban
title = "My site"
baseurl = baseurl ?? "https://example.com"
basepath = "/"
```

The `??` operator means “use the left side unless it’s null”. This lets you provide `baseurl` externally (for example from CI via `--define baseurl=https://staging.example.com`) while having a fallback.

## Base URL and `lunet serve`

`lunet serve` automatically overrides `baseurl` and `basepath` so links point to `http://localhost:4000`. If you need to prevent this (for example, testing with a custom local domain):

```scriban
baseurlforce = true
```

## Common site variables

{.table}
| Variable | Type | Description |
|---|---|---|
| `title` | string | Site title (used by layouts, RSS, cards) |
| `description` | string | Site description (used by cards/RSS) |
| `baseurl` | string | Canonical host URL (e.g. `https://example.com`) |
| `basepath` | string | URL prefix when hosted under a sub-path (e.g. `/docs`) |
| `environment` | string | `"prod"` or `"dev"` (set by CLI, not usually set in config) |
| `layout` | string | Global fallback layout name (tried before `_default`) |
| `url_as_file` | bool | When `true`, keep `*.html` in URLs instead of folder URLs |
| `readme_as_index` | bool | When `true`, `readme.md` behaves like `index.md` for folder URLs |
| `default_page_ext` | string | Default output extension for HTML (`.html` or `.htm`) |

Remember: in `config.scriban`, all of these can be set without the `site.` prefix because the context is the site itself.

## Using modules from config

Most Lunet modules expose a root object that you configure using `with ... end` blocks. These objects are available because the site is the scripting context.

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  concat = true
  minify = true
end
```

The `with bundle ... end` block is equivalent to writing `bundle.css "/css/main.scss"`, `bundle.js "/js/main.js"`, etc. The `with` syntax is a Scriban shorthand for setting multiple properties on an object.

Common module root objects: `bundle`, `resources`, `scss`, `taxonomies`, `search`, `cards`, `markdown`, `menu`, `api`.

See the [Modules reference](plugins/readme.md) for per-module documentation.

## Includes and excludes

Lunet decides whether a file is handled using three glob collections:

- `force_excludes` — cannot be overridden (e.g. `**/.lunet/build/**`, `/config.scriban`)
- `includes` — overrides `excludes` (e.g. `**/.lunet/**` is included by default)
- `excludes` — files matching these globs are skipped unless also matched by `includes`

By default, files and folders starting with `_`, `.`, or `~` are excluded.

You can customize them in config:

```scriban
excludes.add "**/*.psd"
includes.add "**/special-dotfolder/.**"
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

## CLI `--define`

You can inject variables from the command line:

```shell-session
lunet build --define "baseurl=https://staging.example.com"
```

Each `--define` value is executed as a Scriban statement against the site object, so `--define "myvar=42"` sets `site.myvar` to `42`.
