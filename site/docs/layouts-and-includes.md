---
title: "Layouts & includes"
---

# Layouts & includes

Layouts and includes live under `/.lunet/` (in your site or in an extension).

## Includes: `/.lunet/includes/**`

Scriban `include` statements load templates from `/.lunet/includes/`.

```scriban
{{ include "_builtins/head.sbn-html" }}
```

Includes are sandboxed:
- includes cannot start with `/` or `\\`
- includes cannot contain `..`

## Layouts: `/.lunet/layouts/**`

Layouts are resolved by:
- layout name (e.g. `_default`, `docs`, `blog`)
- layout type (usually `single`, sometimes `list` / module-specific types)
- output content type (`.html`, `.xml`, …)

The default layout shipped by Lunet lives at:
- `/.lunet/layouts/_default.sbn-html`

## Layout file naming

Lunet searches for layout files by combining:

- `layout` name (from `page.layout`, `site.layout`, or section name)
- `layout_type` (from `page.layout_type`, default `single`)
- output extension(s) for the page content type

Common patterns:

- `/.lunet/layouts/_default.sbn-html`
- `/.lunet/layouts/docs.single.sbn-html`
- `/.lunet/layouts/docs.list.sbn-html`
- `/.lunet/layouts/tags.term.sbn-html`
- `/.lunet/layouts/_default.rss.xml`
- `/.lunet/layouts/_default.sitemap.xml`

If you ship multiple template flavors, you can also use `scriban-` prefixes:
- `*.scriban-html`
- `*.sbn-html`

## Built-in head rendering

The default layout calls `{{ Head }}` which is provided by `site.builtins.Head`.

You can inject additional snippets (typically via plugins or in config):

```scriban
site.html.head.metas.add "<meta name='theme-color' content='#111'>"
site.html.head.includes.add "_builtins/cards.sbn-html"
```
