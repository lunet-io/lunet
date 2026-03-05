# lunet [![ci](https://github.com/lunet-io/lunet/actions/workflows/ci.yml/badge.svg)](https://github.com/lunet-io/lunet/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/lunet.svg)](https://www.nuget.org/packages/lunet/)

<img align="right" width="196px" height="196px" src="site/img/lunet.png">

Lunet is a fast, modular static website generator for .NET, powered by [Scriban](https://github.com/scriban/scriban) templates.

## ✨ Features

- **Scriban templating** - full scripting language in your pages, layouts, and config (`config.scriban`)
- **Layouts & includes** - automatic layout resolution with section-aware search paths
- **Themes & extensions** - install themes/plugins directly from GitHub repos (`extend "owner/repo@tag"`)
- **npm resources** - fetch and cache npm packages (Bootstrap, Font Awesome…) without a separate `node_modules` workflow
- **Markdown** - [Markdig](https://github.com/xoofx/markdig)-based with cross-reference link support
- **SCSS / Dart Sass** - compile SCSS to CSS with the embedded Dart Sass compiler
- **Bundles** - declarative CSS/JS bundling with automatic minification
- **Taxonomies** - tags, categories, or any custom taxonomy with auto-generated term pages
- **RSS, sitemaps, search** - RSS feeds, `sitemap.xml`, and client-side search index generation
- **SEO & social cards** - OpenGraph / Twitter meta tags from page metadata
- **Data loading** - pull structured data from YAML, JSON, or TOML files into templates
- **Menus** - define navigation trees via simple `menu.yml` files
- **Live reload** - built-in dev server with file watcher and automatic browser refresh
- **Analytics** - Google Analytics injection (production builds only)
- **.NET API docs** - generate API reference pages from .NET projects/assemblies - unique to Lunet
- **URL patterns** - glob-based rules to apply metadata (URLs, layouts, etc.) across pages
- **Summarizer** - automatic page summaries for feeds and cards

## 🚀 Quick start

Install Lunet as a global .NET tool:

```sh
dotnet tool install --global lunet
```

Create and serve a site:

```sh
mkdir mysite && cd mysite
lunet init
lunet serve
```

Your site is live at `http://localhost:4000`. Edit pages and watch changes reload instantly.

Build for production:

```sh
lunet build
```

Output goes to `.lunet/build/www`.

## 🎨 Themes

Install a theme from any GitHub repository:

```
# In config.scriban
extend "owner/repo@v1.0"
```

Themes layer on top of your site - layouts, includes, and static files merge seamlessly.

## 📖 Documentation

Full user guide and module reference at **[lunet.io](https://lunet.io)**.

## 🪪 License

This software is released under the [BSD-2-Clause license](https://github.com/lunet-io/lunet/blob/master/license.txt).

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io)



