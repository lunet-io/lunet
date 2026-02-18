---
title: "SCSS module (Dart Sass)"
---

# SCSS module (Dart Sass)

The SCSS module converts `.scss` and `.sass` files to CSS using [Dart Sass](https://sass-lang.com/dart-sass/), the primary Sass implementation.

## How it works

When Lunet encounters an `.scss` or `.sass` file during the build:

1. The SCSS module compiles it to CSS using Dart Sass.
2. The output file is written with a `.css` extension.
3. If the file is part of a bundle, the compiled CSS is included in the bundle output.

Files that start with `_` (e.g. `_variables.scss`) are treated as partials and are not compiled on their own — they are only included by other SCSS files.

## Configuration

```scriban
with scss
  includes.add "/sass/vendor"    # additional include paths for @use/@import
end
```

### Include paths

SCSS `@use` and `@import` directives resolve relative to the file first, then check each include path. You can add include paths from downloaded resources:

```scriban
$bootstrap = resource "npm:bootstrap" "5.3.8"
scss.includes.add $bootstrap.path + "/scss"
```

## Using SCSS with bundles

The most common way to use SCSS is through a bundle:

```scriban
with bundle
  css "/css/main.scss"
  minify = true
end
```

The SCSS module automatically compiles the file before the bundle processes it. If `minify = true`, the output CSS is also minified.

## Standalone SCSS files

SCSS files outside of bundles are also compiled automatically. For example, `css/custom.scss` in your site folder will produce `css/custom.css` in the output.

## Example: using Bootstrap with SCSS

```scriban
# Download Bootstrap
$bootstrap = resource "npm:bootstrap" "5.3.8"

# Add Bootstrap SCSS to include paths
scss.includes.add $bootstrap.path + "/scss"

# Bundle your custom SCSS that @use's Bootstrap
with bundle
  css "/css/main.scss"
  minify = true
end
```

In `css/main.scss`:

```scss
@use "bootstrap/scss/bootstrap";

// Your custom styles
.my-component {
  @extend .container;
}
```
