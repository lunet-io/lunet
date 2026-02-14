---
discard: true
---

# Website (lunet) Contribution Instructions

This folder contains the static website for Lunet, built with Lunet itself.

## Structure

- `site/readme.md` -> home page (`/`)
- `site/docs/**` -> documentation section (`/docs/**`)
  - `site/docs/menu.yml` -> sidebar menu for docs pages
  - `site/docs/plugins/menu.yml` -> sidebar menu for plugin docs
- `site/menu.yml` -> top navigation
- `site/.lunet/**` -> layouts, includes, cache, and generated output

## Local lunet usage (required)

Use the local build from this repository. Do not use a globally installed `lunet` for this site.

From repository root:

```sh
cd src
dotnet build -c Release
cd ../site
dotnet ../src/Lunet/bin/Release/net10.0/lunet.dll --stacktrace build --dev
```

Run the local server the same way:

```sh
cd site
dotnet ../src/Lunet/bin/Release/net10.0/lunet.dll --stacktrace serve
```

## Notes for agents

- Update menus when adding or moving docs:
  - `site/menu.yml` (top nav)
  - `site/docs/menu.yml` (docs sidebar)
  - `site/docs/plugins/menu.yml` (plugins sidebar)
- Files under `site/docs/todos/**` are internal notes and must keep `discard: true`.

