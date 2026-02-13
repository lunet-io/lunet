---
title: Home
layout: simple
og_type: website
---

# Lunet

Lunet is a fast, scriptable static site generator for .NET.

- Configuration is **Scriban code** (`config.scriban`), not YAML-only config.
- Content and layouts are **Scriban templates**, with optional YAML or Scriban front matter.
- Themes are **extensions** layered on top of your site via `extend "owner/repo@tag"`.

## Quick start

{.table}
| Step | Command |
|---|---|
| Install (as a .NET tool) | `dotnet tool install -g lunet` |
| Create a new site | `lunet init mysite` |
| Build once | `cd mysite && lunet build` |
| Serve + live reload | `cd mysite && lunet serve` |

## Documentation

- [User Guide](docs/readme.md)

