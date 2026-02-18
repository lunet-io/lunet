---
title: "Minifier module"
---

# Minifier module

The minifier module reduces CSS and JavaScript file sizes by removing whitespace, comments, and performing other optimizations. It is used through the [Bundles module](bundles.md) by setting `minify = true`.

## Enable minification

Set `minify = true` on a bundle in `config.scriban`:

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  minify = true
end
```

## How it works

{.table}
| Asset type | Engine | Notes |
|---|---|---|
| JavaScript | [NUglify](https://github.com/trbenning/NUglify) | Removes whitespace, shortens variable names, optimizes expressions |
| CSS | [Dart Sass](https://sass-lang.com/dart-sass/) (`--style=compressed`) | Removes whitespace and comments, merges rules |

Minification is applied **per file**, after SCSS compilation but **before** concatenation (if `concat = true`). Files whose name already ends with `.min.js` or `.min.css` are **skipped** — they are assumed to be pre-minified by their upstream authors.

This means that when you use pre-minified resources (e.g. `bootstrap.bundle.min.js`), only your own source files are run through minification.

When minification is active, output file URLs are renamed with a `.min` extension (e.g. `main.js` → `main.min.js`). This can be customized with the `minify_ext` bundle option.

## Prefer pre-minified files

Whenever a library ships a `.min.js` or `.min.css` build, **use that version** in your bundle rather than relying on the minifier to process the original source. Pre-minified builds are produced by each library's own toolchain and are guaranteed to work correctly.

```scriban
with bundle
  # Recommended: use the pre-minified build
  js bootstrap "/dist/js/bootstrap.bundle.min.js"
end
```

> **Warning**
> NUglify (used for JavaScript) can occasionally produce incorrect output on complex or unusual JavaScript (e.g. advanced syntax, very large files, or edge-case constructs). If your site breaks after enabling `minify = true`, try the following:
>
> 1. Check whether you are minifying a file that already provides a `.min.js` variant — switch to that variant instead.
> 2. Disable minification (`minify = false`) to confirm the problem is minification-related.
> 3. Isolate the problematic file by temporarily removing bundle entries until the build succeeds.
>
> If NUglify produces errors during minification, it logs warnings and **returns the original unminified content** so the build can continue.

## When to enable

- **Production builds** — enable `minify = true` for smaller file sizes.
- **Development** — you can leave it enabled or disabled. `lunet serve` sets `site.environment = "dev"` but does not disable minification automatically; set it conditionally if needed:

```scriban
with bundle
  css "/css/main.scss"
  minify = environment != "dev"
end
```

## See also

- [Bundles module](bundles.md) — defining bundles, concatenation, and resource integration
- [SCSS module](scss.md) — Sass/SCSS compilation (runs before minification)

