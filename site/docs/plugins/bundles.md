---
title: "Bundles module"
---

# Bundles module

Bundles are the main way to:
- collect CSS/JS files,
- optionally concatenate and minify,
- copy referenced content to output,
- inject `<link>` and `<script>` tags via a built-in include.

## Define the default bundle

```scriban
with bundle
  css "/css/main.scss"
  js "/js/main.js"
  concat = true
  minify = true
end
```

By default, the bundle include injects links for `page.bundle` (or the default bundle).

## Named bundles

Create a named bundle:

```scriban
with bundle "docs"
  css "/css/docs.scss"
end
```

Select a bundle per page:

```yaml
bundle: docs
```

## Inject bundle links in layouts

The bundle plugin registers a built-in include:

- `/_builtins/bundle.sbn-html`

Most themes include it from their `<head>` template. If your theme does not, add it:

```scriban
site.html.head.includes.add "_builtins/bundle.sbn-html"
```

## Adding assets from resources

Bundle functions accept either:
- a string path (absolute, virtual path), or
- a resource object returned by `resource`

Examples:

```scriban
$bootstrap = resource "npm:bootstrap" "5.3.8"

with bundle
  css $bootstrap "/dist/css/bootstrap.min.css"
  js $bootstrap "/dist/js/bootstrap.bundle.min.js" mode:""
end
```

## Wildcards and content copies

Use `content` to copy files to an output folder:

```scriban
with bundle
  content "/img/*" "/img/"
end
```

If the path contains `*`, the URL must end with `/`.

## Output folders

Bundles have a `url_dest` map to choose output folders for different kinds:

- `js` defaults to `/js/`
- `css` defaults to `/css/`

You can override:

```scriban
with bundle
  url_dest.js = "/assets/js/"
  url_dest.css = "/assets/css/"
end
```


In page front matter:

```yaml
bundle: docs
```

Then in config, create the bundle:

```scriban
with bundle "docs"
  css "/css/docs.scss"
end
```
