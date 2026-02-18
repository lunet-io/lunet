---
title: "Bundles module"
---

# Bundles module

Bundles are the main way to collect CSS and JavaScript files, concatenate and minify them, copy referenced assets to the output, and inject `<link>` and `<script>` tags into your pages.

## Define the default bundle

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  concat = true
  minify = true
end
```

The default bundle applies to all pages unless a page specifies a different bundle.

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

### How `css` and `js` work with resources

The `css` and `js` functions accept either:

- **A string path** (a virtual path in your site): `css "/css/main.scss"`
- **A resource handle + subpath**: `css bootstrap "/dist/css/bootstrap.min.css"`

When you pass a resource handle, Lunet resolves the file from the downloaded package.

### Copying files with `content`

The `content` function copies files from a resource or your site to the output:

```scriban
# Copy from a resource using a wildcard
content bootstrap_icons "/font/fonts/bootstrap-icons.*" "/fonts/"

# Copy from your site
content "/img/*" "/img/"
```

When the source path contains `*`, the destination must end with `/` (it’s a folder).

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

## Inject bundle links in layouts

The bundle plugin registers a built-in include that outputs `<link>` and `<script>` tags:

```text
_builtins/bundle.sbn-html
```

Most themes include it in their base layout’s `<head>`. If your theme does not, add it from config:

```scriban
site.html.head.includes.add "_builtins/bundle.sbn-html"
```

## Bundle options

{.table}
| Option | Type | Default | Description |
|---|---|---|---|
| `concat` | bool | `false` | Concatenate all CSS/JS files into a single file per type |
| `minify` | bool | `false` | Minify the output (uses NUglify) |
| `url_dest.js` | string | `"/js/"` | Output folder for JavaScript files |
| `url_dest.css` | string | `"/css/"` | Output folder for CSS files |

Override output folders:

```scriban
with bundle
  url_dest.js = "/assets/js/"
  url_dest.css = "/assets/css/"
end
```
