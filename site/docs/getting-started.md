---
title: "Getting started"
---

# Getting started

Lunet is a static site generator that builds a website from a folder containing a `config.scriban` file. This guide walks you through installation, creating your first site, and running it locally.

## Install Lunet

Lunet is distributed as a .NET global tool. You need the [.NET SDK](https://dotnet.microsoft.com/download) (10.0 or later) installed.

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

The skeleton includes:

- A `config.scriban` file (your site configuration)
- Minimal layouts and includes under `.lunet/`
- A sample home page

## Build your site

Build once:

```shell-session
lunet build
```

The output is written to `.lunet/build/www/` by default. Open `index.html` in a browser to see the result.

## Serve with live reload

For development, use the built-in server:

```shell-session
lunet serve
```

This starts a local web server at `http://localhost:4000` with:

- **Live reload** — changes to files are detected and the browser refreshes automatically.
- **Development mode** — `site.environment` is set to `"dev"`, so analytics and other production-only features are disabled.
- **Local base URL** — `baseurl` and `basepath` are overridden to point to `localhost`.

## Key concepts to learn next

1. **[Configuration (`config.scriban`)](configuration.md)** — understand how the config file works as executable Scriban code and how it differs from page templates.
2. **[Content & front matter](content-and-frontmatter.md)** — learn about pages vs static files and how front matter sets page-level variables.
3. **[Site structure](site-structure.md)** — understand the folder layout and the layered virtual filesystem.
4. **[Layouts & includes](layouts-and-includes.md)** — learn how Lunet wraps your content in reusable templates.
5. **[Themes & extensions](themes-and-extends.md)** — use pre-built themes or create your own.
