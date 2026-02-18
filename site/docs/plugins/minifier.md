---
title: "Minifier module"
---

# Minifier module

The minifier module reduces CSS and JavaScript file sizes by removing whitespace, comments, and performing other optimizations. It plugs into the [Bundles module](bundles.md).

## Enable minification

Set `minify = true` on a bundle:

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  minify = true
end
```

## How it works

{.table}
| Asset type | Minifier | Notes |
|---|---|---|
| JavaScript | NUglify | Removes whitespace, shortens variable names, optimizes expressions |
| CSS | NUglify | Removes whitespace and comments, merges rules |

Minification runs after concatenation (if `concat = true`) and after SCSS compilation, so the final output is a single minified file per asset type.

## When to enable

- **Production builds** — enable `minify = true` for smaller file sizes.
- **Development** — you can leave it enabled or disabled. `lunet serve` sets `site.environment = "dev"` but does not disable minification automatically; set it conditionally if needed:

```scriban
with bundle
  css "/css/main.scss"
  minify = environment != "dev"
end
```

