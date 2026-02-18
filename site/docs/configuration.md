---
title: "Configuration (config.scriban)"
---

# Configuration (`config.scriban`)

Unlike most static site generators, Lunet's configuration file is **executable Scriban code**, not a passive data file like YAML or TOML.

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

Because `config.scriban` runs "inside" the site, you can refer to any site property without a prefix. The `site.` prefix is optional but can be used for clarity.

> [!IMPORTANT]
>
> This is different from page templates, where you must use `site.title` to read the site title and `page.title` for the page title. See [Content & front matter](content-and-frontmatter.md) for the page context rules.

### Config vs page context (quick comparison)

{.table}
| Context | Scripting target | `title = "x"` sets… | Access site title as… | `include` allowed? |
|---|---|---|---|---|
| `config.scriban` | SiteObject (the site) | `site.title` | `title` or `site.title` | No |
| Page front matter | ContentObject (the page) | `page.title` | `site.title` | No |
| Page/layout body | — (both `site` and `page` in scope) | — (use `{{ '{{' }}` `…` `{{ '}}' }}`) | `site.title` | Yes (from `/.lunet/includes/`) |

### What runs when

`config.scriban` runs **once** at the start of every build, before any content is loaded or processed.

The full initialization sequence is:

1. **CLI `--define` values** are evaluated first (as Scriban statements against the site object). This is why `??` in config works — the define has already set the variable.
2. **Plugins are instantiated** — each plugin registers its own objects and functions on the site (e.g. `bundle`, `search`, `cards`).
3. **`config.scriban` is evaluated** — your config code runs with the site as the scripting context.
4. **Content loading** — pages, data files, and static files are loaded.
5. **Content processing** — the pipeline runs converters, layouts, and post-processors.

This means:

- You cannot access `site.pages` from config (pages haven't been loaded yet).
- Module configuration in config takes effect before the content pipeline runs.
- Extensions loaded with `extend` are imported during config execution, so their `config.scriban` also runs at this stage.

### Scriban scope chain

During config execution, variable lookup follows this scope chain (top to bottom):

```text
1. SiteObject            ← config.scriban runs here; bare variables resolve here
2. Builtins              ← log, lunet, extend, resource, bundle, defer, etc.
3. Scriban built-in functions  ← string, math, date, array, etc.
```

This is why `log.info "text"` works without a `builtins.` prefix — `log` is found on the builtins layer. You can also write `builtins.log.info "text"` explicitly.

## Minimal config

```scriban
title = "My site"
baseurl = baseurl ?? "https://example.com"
basepath = "/"
```

The `??` operator means "use the left side unless it's null". This lets you provide `baseurl` externally (for example from CI via `--define baseurl=https://staging.example.com`) while having a fallback.

## Base URL and `lunet serve`

`lunet serve` automatically overrides `baseurl` and `basepath` so links point to `http://localhost:4000`. If you need to prevent this (for example, testing with a custom local domain):

```scriban
baseurlforce = true
```

## Common site variables

{.table}
| Variable | Type | Default | Description |
|---|---|---|---|
| `title` | string | (none) | Site title; used by layouts, [RSS](plugins/rss.md), [Cards](plugins/cards.md). Not a built-in property — any value assigned is stored dynamically. |
| `description` | string | (none) | Site description; used by [Cards](plugins/cards.md) and [RSS](plugins/rss.md). Also dynamic. |
| `baseurl` | string | `null` | Canonical host URL (e.g. `https://example.com`). Overridden by `lunet serve` unless `baseurlforce` is `true`. |
| `basepath` | string | `null` | URL prefix when hosted under a sub-path (e.g. `/docs`). |
| `baseurlforce` | bool | `false` | When `true`, `lunet serve` does not override `baseurl` and `basepath`. |
| `error_redirect` | string | `"/404.html"` | Path served by `lunet serve` for HTTP 404 errors. See [Server module](plugins/server.md). |
| `environment` | string | `"prod"` | Set by the CLI (`--dev` → `"dev"`, `lunet serve` → `"dev"`). Rarely set in config. |
| `layout` | string | `null` | Global fallback layout name; tried between section-specific and `_default` layouts. See [Layouts & includes](layouts-and-includes.md). |
| `url_as_file` | bool | `false` | When `true`, keep `*.html` in URLs instead of folder URLs. |
| `readme_as_index` | bool | `true` | When `true`, `readme.md` behaves like `index.md` for folder URLs. |
| `default_page_ext` | string | `".html"` | Default output extension for HTML pages. Must be `".html"` or `".htm"`. |

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

Common module root objects: `bundle`, `resources`, `scss`, `taxonomies`, `search`, `cards`, `markdown`, `menu`, `api`, `tracking`, `rss`.

See the [Modules reference](plugins/readme.md) for per-module documentation.

## Includes and excludes

Lunet decides whether a file is handled using three glob collections evaluated in this order:

1. **`force_excludes`** — if a file matches, it is **excluded** and cannot be overridden.
2. **`includes`** — if a file matches, it is **included** (overrides `excludes`).
3. **`excludes`** — if a file matches, it is **excluded**.
4. Otherwise — the file is **included**.

### Default patterns

{.table}
| Collection | Default patterns | Effect |
|---|---|---|
| `force_excludes` | `**/.lunet/build/**`, `/config.scriban` | Build output and config are never processed as content |
| `excludes` | `**/~*/**`, `**/.*/**`, `**/_*/**` | Folders/files starting with `~`, `.`, or `_` are skipped |
| `includes` | `**/.lunet/**` | The `.lunet/` folder is included despite starting with `.` |

You can customize them in config:

```scriban
excludes.add "**/*.psd"
includes.add "**/special-dotfolder/.**"
```

> [!NOTE]
>
> The `force_excludes`, `excludes`, and `includes` properties on the site are registered as read-only references (you cannot reassign them with `=`), but their `.add` and `.clear` methods work normally.

## Logging from config

Use the built-in `log` object:

```scriban
log.info "Config loaded"
log.warn "Something looks off"
log.debug "Detailed trace info"
```

Available log methods: `trace`, `debug`, `info`, `warn`, `error`, `fatal`.

Control verbosity:

```scriban
builtins.log.level = "debug"
```

Accepted level values: `trace`, `debug`, `info` (or `information`), `warn` (or `warning`), `error`, `fatal` (or `critical`). Default: `info`.

## CLI `--define`

You can inject variables from the command line:

```shell-session
lunet build --define "baseurl=https://staging.example.com"
```

Each `--define` value is executed as a Scriban statement against the site object, so `--define "myvar=42"` sets `site.myvar` to `42`. Defines are evaluated **before** `config.scriban` runs, which is why the `??` pattern works.

See [CLI reference](cli.md) for full command-line documentation.

## Scriban language patterns in config

Since `config.scriban` is executable Scriban code, you can use the full Scriban language. Here are the most useful patterns.

### Null coalescing (`??`)

The `??` operator returns the left side if it's not null, otherwise the right side:

```scriban
baseurl = baseurl ?? "https://example.com"
```

This is commonly used to provide defaults that can be overridden by `--define` or by an extension.

### String interpolation (`$"..."`)

Use `$"..."` for string interpolation inside config:

```scriban
github_user = "my-org"
github_repo = "my-project"
github_repo_url = $"https://github.com/{github_user}/{github_repo}/"
```

### Conditional expressions (`? :`)

The ternary operator works inside config:

```scriban
minify_output = environment != "dev"
```

### `with` blocks

The `with ... end` block sets a context for setting multiple properties on an object:

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  concat = true
  minify = true
end
```

`with` blocks can be nested:

```scriban
with cards
  with twitter
    enable = true
    card = "summary_large_image"
  end
  with og
    enable = true
  end
end
```

### `for` loops

You can loop in config to add multiple items:

```scriban
prism_components = ["prism-csharp.min.js", "prism-python.min.js", "prism-json.min.js"]

with bundle
  for path in prism_components
    content prismjs "components/" + path "/js/components/"
  end
end
```

### Defining functions (`func`)

You can define reusable functions in config. This is commonly used by themes to provide an initialization function:

```scriban
func site_project_init
  title = site_project_name ?? "My Project"
  description = site_project_description ?? "Project description."
  baseurl = baseurl ?? site_project_baseurl ?? "https://example.com"
  author = site_project_owner_name
end
```

The consumer site calls this function after setting the input variables:

```scriban
extend "owner/theme-repo@1.0.0"

site_project_name = "My App"
site_project_description = "A great app."
site_project_baseurl = "https://myapp.io"
site_project_owner_name = "Jane Doe"

site_project_init   # calls the function defined by the theme
```

### Computed properties with `do`/`ret`

Some site properties accept a **deferred expression** using `do; ret ...; end`. This creates a function that is evaluated later (at render time) instead of immediately:

```scriban
html.head.title = do
  ret (page.title == "Home" ? site.title : page.title + " | " + site.title)
end
```

This is particularly useful for `html.head.title` because `page` is only available at render time, not during config.

### Adding to collections

Many configuration objects expose collections you can add to:

```scriban
# Add meta tags to <head>
html.head.metas.add '<meta name="author" content="Jane Doe">'

# Add includes to <head>
html.head.includes.add "_builtins/cards.sbn-html"

# Add search excludes
search.excludes.add ["/draft/**", "/internal/**"]

# Add SCSS include paths
scss.includes.add "/sass/vendor"
```

## Configuring the HTML document (`site.html`)

The `site.html` object controls what gets injected into the HTML document shell by the base layout. This is configured in `config.scriban` and consumed at render time by includes like `_builtins/head.sbn-html`.

### Object structure

```text
html
├── attributes        ← string injected on <html> element
├── head
│   ├── title         ← supports do/ret deferred expressions
│   ├── metas         ← collection of <meta>/<link>/<script> strings
│   └── includes      ← collection of Scriban include paths rendered in <head>
└── body
    ├── attributes    ← string injected on <body> element
    └── includes      ← collection of Scriban include paths rendered in <body>
```

### Default `<head>` metas

The following metas are added by default:

- `<meta charset="utf-8">`
- `<meta name="viewport" content="width=device-width, initial-scale=1">`
- `<meta name="generator" content="lunet ...">`

### Default `<head>` includes

Plugins automatically register their includes in `html.head.includes`:

- `_builtins/bundle.sbn-html` — CSS/JS bundle injection ([Bundles module](plugins/bundles.md))
- `_builtins/cards.sbn-html` — OpenGraph/Twitter meta tags ([Cards module](plugins/cards.md))
- `_builtins/google-analytics.sbn-html` — analytics script ([Tracking module](plugins/tracking.md))

### Custom `<title>`

Use a `do`/`ret` block to compute the title at render time (when `page` is available):

```scriban
html.head.title = do
  ret (page.title == "Home" ? site.title : page.title + " | " + site.title)
end
```

### HTML element attributes

Add attributes to the `<html>` element:

```scriban
html.attributes = 'lang="en" itemscope itemtype="http://schema.org/WebPage"'
```

You can include `data-` attributes for JavaScript initialization:

```scriban
html.attributes = 'data-theme-mode="system" data-theme-key="my-theme" lang="en"'
```

### Inline scripts and meta tags

Add inline `<script>` or `<meta>` tags to `<head>`:

```scriban
html.head.metas.add '<link rel="icon" href="/favicon.ico" sizes="32x32">'
html.head.metas.add '<script>/* early inline JS, e.g. theme flicker prevention */</script>'
```

### Head includes (template fragments)

Register Scriban include templates to be rendered inside `<head>`:

```scriban
html.head.includes.add "_builtins/cards.sbn-html"
html.head.includes.add "_builtins/bundle.sbn-html"
```

These includes have access to `site`, `page`, and all template variables at render time.

### Body attributes

Add attributes to the `<body>` element:

```scriban
html.body.attributes = 'class="docs-page"'
```

## Built-in objects and helpers

### `builtins.lunet`

{.table}
| Property | Description |
|---|---|
| `lunet.version` | The current Lunet version string |

### `builtins.defer`

Creates a deferred evaluation marker. Used internally; prefer `do/ret` blocks for deferred expressions in config.

### Built-in Scriban functions

All [standard Scriban built-in functions](https://github.com/scriban/scriban/blob/master/doc/builtins.md) are available: `string`, `math`, `date`, `array`, `object`, `regex`, `html`, `timespan`.

Lunet adds `date.to_rfc822` for RFC 822 date formatting (used internally by the [RSS module](plugins/rss.md)).

## See also

- [CLI reference](cli.md) — `--define`, `--dev`, and other command-line options
- [Content & front matter](content-and-frontmatter.md) — page-level context vs config context
- [Layouts & includes](layouts-and-includes.md) — how config settings affect layout resolution
- [Themes & extensions](themes-and-extends.md) — `extend` and config execution order
- [Site structure](site-structure.md) — the `.lunet/` folder and layered filesystem
- [Modules reference](plugins/readme.md) — per-module configuration documentation
