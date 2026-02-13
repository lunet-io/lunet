---
title: "Minifier module"
---

# Minifier module

The minifier module plugs into bundles to minify output assets.

- JS: minified via NUglify
- CSS: minified via Dart Sass minification

Enable minification on a bundle:

```scriban
with bundle
  minify = true
end
```

