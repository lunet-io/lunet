---
title: "Getting started"
---

# Getting started

Lunet builds a site from a folder that contains a `config.scriban` file.

## Install Lunet

Install Lunet as a global .NET tool:

```shell-session
dotnet tool install -g lunet
```

Verify:

```shell-session
lunet --help
```

## Create a new site

Create a new site using the built-in skeleton:

```shell-session
lunet init mysite
cd mysite
```

The skeleton includes a `config.scriban` and minimal layouts under `/.lunet/`.

## Build your site

Build once:

```shell-session
lunet build
```

The output is written under `.lunet/build/www/` by default.

## Serve with live reload

Run the development server:

```shell-session
lunet serve
```

`lunet serve` defaults to:
- environment = `dev`
- baseurl overridden to `http://localhost:4000`
- live reload enabled

## Next

- [CLI reference](cli.md)
- [Configuration (`config.scriban`)](configuration.md)
- [Themes & extensions](themes-and-extends.md)

