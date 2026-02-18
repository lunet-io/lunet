---
title: Home
layout: simple
og_type: website
---

# Lunet

Lunet is a fast, scriptable static site generator for .NET, powered by [Scriban](https://github.com/scriban/scriban).

- Configuration is **executable Scriban code** (`config.scriban`), not YAML-only config.
- Content files use **Scriban templates** with optional YAML or Scriban front matter.
- Themes are **extensions** layered on top of your site via `extend "owner/repo@tag"`.
- A **virtual layered filesystem** lets you override any theme file by placing your own version at the same path.

> [!WARNING]
> 
> This documentation is a work in progress and not yet stable. Expect breaking changes until the 1.0 release.

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
- [.NET API Reference sample](api/readme.md)
