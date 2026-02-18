---
title: "Lunet — User Guide"
---

# Lunet — User Guide

This guide documents Lunet from a **user perspective**: how to create a site, configure it, and use each built-in module.

## First steps

{.table}
| Guide | What you'll learn |
|---|---|
| [Getting started](getting-started.md) | Install Lunet, create a site, and run `lunet build`/`lunet serve` |
| [CLI reference](cli.md) | `init`, `clean`, `build`, `serve` and their options |
| [Site structure](site-structure.md) | What lives in your site folder and what Lunet generates under `.lunet/` |

## Core concepts

{.table}
| Guide | What you'll learn |
|---|---|
| [Configuration (`config.scriban`)](configuration.md) | How the config script runs and how to use Scriban to drive builds |
| [Content & front matter](content-and-frontmatter.md) | Pages vs static files, YAML vs `+++` Scriban front matter, common page variables |
| [Layouts & includes](layouts-and-includes.md) | Layout resolution in `/.lunet/layouts`, includes in `/.lunet/includes` |
| [Themes & extensions](themes-and-extends.md) | `extend "owner/repo@tag"`, layering rules, conventions (`dist/`, `.lunet/`) |

## Modules (plugins)

{.table}
| Module | What it does |
|---|---|
| [Extends (themes)](plugins/extends.md) | Download + layer themes/extensions |
| [Resources (npm)](plugins/resources.md) | Download + cache external assets |
| [Bundles](plugins/bundles.md) | Build CSS/JS bundles and copy content |
| [Markdown](plugins/markdown.md) | Convert Markdown to HTML (+ xref support) |
| [SCSS (Dart Sass)](plugins/scss.md) | Compile SCSS to CSS |
| [Minifier](plugins/minifier.md) | Minify JS/CSS (used by bundles) |
| [Summarizer](plugins/summarizer.md) | Compute `page.summary` for feeds/cards |
| [Menus](plugins/menus.md) | Define navigation via `menu.yml` |
| [Taxonomies](plugins/taxonomies.md) | Tags/categories + generated term pages |
| [RSS](plugins/rss.md) | Generate RSS feeds via layouts |
| [Sitemaps](plugins/sitemaps.md) | Generate `sitemap.xml` + `robots.txt` |
| [Search](plugins/search.md) | Generate a client-side search index |
| [Cards (OpenGraph/Twitter)](plugins/cards.md) | SEO/social meta tags |
| [Tracking (Google Analytics)](plugins/tracking.md) | Analytics injection (prod only) |
| [Server (`lunet serve`)](plugins/server.md) | Local web server + live reload |
| [Watcher (`--watch`)](plugins/watcher.md) | File watcher + incremental rebuild |
| [Data modules overview](plugins/data.md) | Supported data formats at a glance |
| [Datas](plugins/datas.md) | Load data files from `/.lunet/data` into `site.data` |
| [YAML](plugins/yaml.md) | YAML front matter + YAML data loading |
| [JSON](plugins/json.md) | JSON data loading |
| [TOML](plugins/toml.md) | TOML data loading |
| [Attributes (URL patterns)](plugins/attributes.md) | Apply per-path metadata like `url` patterns |
| [API](plugins/api.md) | API-documentation registry (`site.api`) |
| [API (.NET)](plugins/api-dotnet.md) | Generate .NET API docs from projects/assemblies |

## Internal notes (not published)

- Internal refactoring/testing notes live under `docs/todos/` and are marked as `discard: true`.
