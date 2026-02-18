---
title: "Bundles module"
---

# Bundles module

Bundles are the main way to collect CSS and JavaScript files, concatenate and minify them, copy referenced assets to the output, and inject `<link>` and `<script>` tags into your pages.

All bundle configuration is done in `config.scriban` using the `with bundle` block.

## Define the default bundle

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  concat = true
  minify = true
end
```

The default bundle (internally named `"site"`) applies to all pages unless a page specifies a different bundle in its front matter.

## Adding CSS files

The `css` function adds a stylesheet to the bundle:

```scriban
with bundle
  css "/css/main.scss"
end
```

With a resource handle (see [Resources module](resources.md)):

```scriban
with bundle
  $bootstrap = resource "npm:bootstrap" "5.3.8"
  css $bootstrap "/dist/css/bootstrap.min.css"
end
```

SCSS files (`.scss`) are automatically compiled to CSS by the [SCSS module](scss.md) before bundling.

## Adding JavaScript files

The `js` function adds a script to the bundle:

```scriban
with bundle
  js "/js/main.js"
end
```

With a resource handle:

```scriban
with bundle
  $bootstrap = resource "npm:bootstrap" "5.3.8"
  js $bootstrap "/dist/js/bootstrap.bundle.min.js"
end
```

By default, all `<script>` tags are emitted with the `defer` attribute. You can change this by passing an explicit mode as the last argument:

```scriban
with bundle
  js "/js/main.js"                # defer (default)
  js "/js/analytics.js" "async"   # async
  js "/js/critical.js" ""         # no attribute (blocking)
end
```

When `concat = true`, scripts with different modes are concatenated into **separate output files** (e.g. `site-defer.js`, `site-async.js`).

## Copying files with `content`

The `content` function copies files from a resource or your site to the output. Content files are **never concatenated or minified** — they are raw file copies.

```scriban
with bundle
  # Copy from a resource using a wildcard
  $icons = resource "npm:bootstrap-icons" "1.13.1"
  content $icons "/font/fonts/bootstrap-icons.*" "/fonts/"

  # Copy from your site
  content "/img/*" "/img/"
end
```

When the source path contains `*`, the destination must end with `/` (it is treated as a folder).

## Using resources (npm packages) with bundles

A common pattern is to download npm packages with the `resource` function and use them in bundles. You can store resource handles as variables inside the `with bundle` block:

```scriban
with bundle
  # Download resources (cached locally)
  bootstrap = resource "npm:bootstrap" "5.3.8"
  bootstrap_icons = resource "npm:bootstrap-icons" "1.13.1"
  tocbot = resource "npm:tocbot" "4.36.4"

  # Add SCSS include paths from resources
  scss.includes.add bootstrap.path + "/scss"
  scss.includes.add bootstrap_icons.path + "/font"

  # Add CSS files from resources
  css tocbot "/dist/tocbot.css"
  css "/css/main.scss"

  # Add JS files from resources
  js bootstrap "/dist/js/bootstrap.bundle.min.js"
  js tocbot "/dist/tocbot.min.js"
  js "/js/main.js"

  # Copy font files from a resource to the output
  content bootstrap_icons "/font/fonts/bootstrap-icons.*" "/fonts/"

  concat = true
  minify = true
end
```

## Named bundles

Create a named bundle for pages that need different assets:

```scriban
with bundle "docs"
  css "/css/docs.scss"
end
```

Select it in page front matter:

```yaml
bundle: docs
```

> **Note:** Only bundles that are actually referenced by at least one page are processed during the build. A bundle defined in `config.scriban` but not used by any page is ignored.

## Inject bundle links in layouts

The bundle plugin automatically registers a built-in include that outputs `<link>` and `<script>` tags into the `<head>` of your pages:

```text
_builtins/bundle.sbn-html
```

CSS `<link>` tags are emitted first, then `<script>` tags. For JavaScript, the `mode` attribute is applied (e.g. `defer`, `async`). Content-type entries (file copies) do not produce any HTML tags.

Most themes include this automatically. If your theme does not, add it from `config.scriban`:

```scriban
site.html.head.includes.add "_builtins/bundle.sbn-html"
```

## Bundle options

All options are set inside the `with bundle` block in `config.scriban`:

{.table}
| Option | Type | Default | Description |
|---|---|---|---|
| `concat` | bool | `false` | Concatenate all CSS/JS files into a single file per type and mode |
| `minify` | bool | `false` | Minify each file individually (see [Minifier module](minifier.md)) |
| `minify_ext` | string | `".min"` | Extension inserted before file type when minifying (e.g. `.min.js`) |
| `minifier` | string | `null` | Name of the minifier to use; `null` picks the first registered minifier |
| `url_dest.js` | string | `"/js/"` | Output folder for JavaScript files |
| `url_dest.css` | string | `"/css/"` | Output folder for CSS files |

Override output folders:

```scriban
with bundle
  url_dest.js = "/assets/js/"
  url_dest.css = "/assets/css/"
end
```

## Processing pipeline

When a bundle is processed, the following steps happen in order:

1. **Wildcard expansion** — links with `*` in their path are expanded to individual files.
2. **Content processing** — each file is processed by content processors (e.g. SCSS compilation).
3. **Minification** — if `minify = true`, each file is minified individually. Files ending with `.min.js` or `.min.css` are skipped (see [Minifier module](minifier.md)).
4. **Source map removal** — `sourceMappingURL` comments are stripped from all files.
5. **Concatenation** — if `concat = true`, files are merged per type and mode into output files named `{bundle}.{type}` (e.g. `site.css`, `site-defer.js`).

> **Note:** During CSS concatenation, `@charset` rules are deduplicated — only the first one is kept at the top of the output.

## See also

- [Minifier module](minifier.md) — CSS/JS minification
- [SCSS module](scss.md) — Sass/SCSS compilation
- [Resources module](resources.md) — downloading npm packages for use in bundles
