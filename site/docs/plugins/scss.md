---
title: "SCSS module (Dart Sass)"
---

# SCSS module (Dart Sass)

The SCSS module compiles `.scss` files to CSS using [Dart Sass](https://sass-lang.com/dart-sass/), the primary Sass implementation. It is most commonly used through the [Bundles module](bundles.md).

## How it works

When Lunet encounters an `.scss` file during the build (either standalone or as part of a bundle), the SCSS module:

1. Compiles it to CSS using Dart Sass.
2. Changes the output extension to `.css`.
3. If the file is part of a bundle, the compiled CSS is passed along for concatenation and/or minification.

Dart Sass is automatically downloaded on first use (version 1.94.0) and cached locally. No manual installation is required.

> **Note:** Only `.scss` files are compiled. The indented `.sass` syntax is not currently supported by the SCSS module.

## Configuration

Include paths are configured in `config.scriban`. The `scss` and `sass` aliases both refer to the same plugin instance:

```scriban
scss.includes.add "/sass/vendor"
# or equivalently:
sass.includes.add "/sass/vendor"
```

### Include paths

SCSS `@use` and `@import` directives resolve relative to the source file first, then check each additional include path. Include paths must be valid directories in the site or its extensions.

You can add include paths from downloaded resources:

```scriban
with bundle
  $bootstrap = resource "npm:bootstrap" "5.3.8"
  scss.includes.add $bootstrap.path + "/scss"
end
```

## Using SCSS with bundles

The most common way to use SCSS is through a bundle in `config.scriban`:

```scriban
with bundle
  css "/css/main.scss"
  concat = true
  minify = true
end
```

The SCSS module automatically compiles the file before the bundle processes it. When `minify = true`, CSS minification is also performed by Dart Sass (using `--style=compressed`), not NUglify.

## Standalone SCSS files

SCSS files outside of bundles are also compiled automatically. For example, `css/custom.scss` in your site folder will produce `css/custom.css` in the output.

## CSS minification

When the [Minifier module](minifier.md) minifies CSS files, it delegates to Dart Sass (`--style=compressed`) rather than NUglify. This means CSS minification benefits from the same well-tested Sass engine used for compilation.

## Example: using Bootstrap with SCSS

In `config.scriban`:

```scriban
with bundle
  $bootstrap = resource "npm:bootstrap" "5.3.8"
  scss.includes.add $bootstrap.path + "/scss"

  css "/css/main.scss"
  concat = true
  minify = true
end
```

In your `css/main.scss`:

```scss
@use "bootstrap/scss/bootstrap";

// Your custom styles
.my-component {
  @extend .container;
}
```

## See also

- [Bundles module](bundles.md) — bundling, concatenation, and asset management
- [Minifier module](minifier.md) — CSS/JS minification (CSS uses Dart Sass)
- [Resources module](resources.md) — downloading npm packages for SCSS include paths
