---
title: "Themes & extensions (extend)"
---

# Themes & extensions (`extend`)

Extensions (themes) are Lunet’s way to layer a reusable site template on top of your site.

An extension is typically a GitHub repository with:

- a `dist/` folder containing the theme content
- an optional `dist/config.scriban` that runs when the extension is loaded
- an optional `dist/.lunet/` folder for layouts/includes/data shipped by the theme

## Using a GitHub extension

Use the latest `main`:

```scriban
extend "owner/repo"
```

Pin a specific tag:

```scriban
extend "owner/repo@1.0.0"
```

By convention, Lunet extracts the `dist/` folder from the repository and layers it as a content filesystem.

## Local theme development

To iterate on a theme without publishing to GitHub, put it under your site’s:

`/.lunet/extends/<name>/`

Then load it by name:

```scriban
extend "mytheme"
```

## Override rules

Your local site wins when the same path exists in multiple layers.

Common override patterns:
- override a theme layout by creating `/.lunet/layouts/...` locally
- override a theme include by creating `/.lunet/includes/...` locally
- override a theme page by creating the same path in your site

## Local extensions (advanced)

Lunet also supports local extensions under `/.lunet/extends/` in your site. This is useful when developing a theme locally without pushing to GitHub.
