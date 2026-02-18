---
title: "Getting started"
---

# Getting started

Lunet is a static site generator that builds a website from a folder containing a `config.scriban` file. This guide walks you through installation, creating your first site, and running it locally.

## Prerequisites

Lunet requires the **.NET 10 SDK** (or later). If you don't have it yet, download it from:

<https://dotnet.microsoft.com/en-us/download/dotnet/10.0>

## Install Lunet

Lunet is distributed as a .NET global tool:

```shell-session
dotnet tool install -g lunet
```

Verify the installation:

```shell-session
lunet --help
```

## Create a new site

Create a new site using the built-in skeleton:

```shell-session
lunet init mysite
cd mysite
```

The skeleton uses the [default Lunet template](https://github.com/lunet-io/templates) and creates:

- **`config.scriban`** — site configuration that extends `lunet-io/templates` with project metadata
- **`readme.md`** — a sample home page
- **`menu.yml`** — top-level navigation (Home + Docs)
- **`docs/readme.md`** — a starter documentation page
- **`docs/menu.yml`** — sidebar navigation for the docs section

The template provides layouts, includes, CSS/JS assets, a theme switcher, and search — your site is fully functional out of the box. See the [template readme](https://github.com/lunet-io/templates) for all configuration options.

## Build your site

Build once:

```shell-session
lunet build
```

The output is written to `.lunet/build/www/` by default. Open `index.html` in a browser to see the result.

For a production build, Lunet sets `site.environment = "prod"`. To build in development mode:

```shell-session
lunet build --dev
```

## Serve with live reload

For development, use the built-in server:

```shell-session
lunet serve
```

This starts a local web server at `http://localhost:4000` with:

- **Live reload** — changes to files are detected and the browser refreshes automatically via WebSocket.
- **Development mode** — `site.environment` is set to `"dev"`, so production-only features (like [analytics](plugins/tracking.md)) are disabled.
- **Local base URL** — `baseurl` and `basepath` are overridden to point to `localhost`.

## Adding content

Create Markdown files with YAML front matter to add pages:

```markdown
---
title: "About"
---

# About this site

This is a Markdown page that will be converted to HTML.
```

Files with front matter are treated as **pages** and processed through the layout pipeline. Files without front matter (images, CSS, JS) are copied to the output as-is.

See [Content & front matter](content-and-frontmatter.md) for details on front matter formats and page variables.

## Key concepts to learn next

1. **[Configuration (`config.scriban`)](configuration.md)** — understand how the config file works as executable Scriban code and how it differs from page templates.
2. **[Content & front matter](content-and-frontmatter.md)** — learn about pages vs static files and how front matter sets page-level variables.
3. **[Site structure](site-structure.md)** — understand the folder layout and the layered virtual filesystem.
4. **[Layouts & includes](layouts-and-includes.md)** — learn how Lunet wraps your content in reusable templates.
5. **[Themes & extensions](themes-and-extends.md)** — use pre-built themes or create your own.
6. **[CLI reference](cli.md)** — full command and option reference.

## See also

- [Modules reference](plugins/readme.md) — all built-in modules ([Bundles](plugins/bundles.md), [SCSS](plugins/scss.md), [Markdown](plugins/markdown.md), [Menus](plugins/menus.md), and more)
