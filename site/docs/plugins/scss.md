---
title: "SCSS module (Dart Sass)"
---

# SCSS module (Dart Sass)

The SCSS module converts `.scss` files to CSS. For Lunet’s public documentation, prefer **Dart Sass**.

## Enable Dart Sass

```scriban
with scss
  use_dart_sass = true
end
```

## Include paths

In practice, you usually populate include paths from resources:

```scriban
$bootstrap = resource "npm:bootstrap" "5.3.8"
scss.includes.add $bootstrap.path + "/scss"
```

## Add SCSS to a bundle

```scriban
with bundle
  css "/css/main.scss"
end
```

