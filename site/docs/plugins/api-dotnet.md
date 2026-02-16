---
title: "API module (.NET)"
---

# API module (.NET)

Lunet generates .NET API pages from one or more `.csproj` files with `api.dotnet`.

## Quick start (`config.scriban`)

```scriban
with api.dotnet
  title = "MyProject API Reference"
  path = "/api"
  menu_name = "api"
  menu_title = "API Reference"
  config = "Release"
  properties = { TargetFramework: "net10.0" }
  projects = [
    { name: "MyProject", path: "../src/MyProject/MyProject.csproj" }
  ]
end
```

This generates API pages under `path` (default `/api`) and registers all API UIDs for `xref:` links.

## Configuration reference

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `api.dotnet.title` | string | `"<site title> .NET API Reference"` | Title of the generated API root page |
| `api.dotnet.path` | string | `"/api"` | Root URL/path for generated API pages |
| `api.dotnet.menu_name` | string | `"api"` | Menu name exposed on `site.menu.<name>` |
| `api.dotnet.menu_title` | string | `api.dotnet.title` | Root menu title |
| `api.dotnet.menu_width` | int | `4` | Sidebar width hint for this menu (clamped to `2..4`) |
| `api.dotnet.config` | string | `"Release"` | Build configuration passed to `dotnet build` |
| `api.dotnet.properties` | object | empty | Extra MSBuild properties (`-p:`), e.g. `TargetFramework` |
| `api.dotnet.projects` | array | required | Projects to extract (`string` or object entries) |
| `api.dotnet.references` | array<string> | empty | Referenced assemblies to include for all projects |
| `api.dotnet.include_helper` | string | `"_builtins/api-dotnet-helpers.sbn-html"` | Helper include loaded for generated API pages |
| `api.dotnet.layout` | string | `"_default"` | Base layout used by generated API pages |
| `api.dotnet.table_class` | string | `"table table-striped table-hover table-sm api-dotnet-members-table"` | CSS classes used by default API member tables |

Project entry object:

{.table}
| Key | Type | Required | Description |
|---|---|---|---|
| `name` | string | no | Logical project name (cache id). Defaults to project filename |
| `path` | string | yes | Path to a `.csproj` (or glob pattern) |
| `properties` | object | no | Per-project MSBuild properties (override `api.dotnet.properties`) |
| `references` | array<string> / string | no | Per-project referenced assemblies to include |

## Generated pages

Lunet creates dynamic pages:

- `<path>/` with `layout_type = "api-dotnet"` (API root)
- `<path>/<namespace-uid>/` with `layout_type = "api-dotnet-namespace"`
- `<path>/<member-uid>/` with `layout_type = "api-dotnet-member"`

Default templates:

- `src/Lunet.Api.DotNet/shared/.lunet/layouts/_default.api-dotnet.sbn-md`
- `src/Lunet.Api.DotNet/shared/.lunet/layouts/_default.api-dotnet-namespace.sbn-md`
- `src/Lunet.Api.DotNet/shared/.lunet/layouts/_default.api-dotnet-member.sbn-md`
- `src/Lunet.Api.DotNet/shared/.lunet/includes/_builtins/api-dotnet-helpers.sbn-html`

Override by adding files with the same names under `site/.lunet/layouts` or `site/.lunet/includes`.

By default, generated API lists use Bootstrap table classes (`.table`, `.table-striped`, `.table-hover`, `.table-sm`).

## Automatic menu integration

`api.dotnet` now builds a menu tree automatically:

- root (`menu_title`)
- namespaces
- namespace members (types)
- type member groups (constructors, fields, properties, methods, events, operators, extensions, explicit interface implementations)
- actual member pages inside each group

The generated menu is available as `site.menu.<menu_name>` and every API page receives:

- `page.menu_item` (current menu node)
- `page.menu` (renderable menu root/subtree like normal menus)

You can therefore render API menus with the same helpers as `menu.yml` menus.

Generated API menus include Bootstrap icon markup (`bi-*`) for namespaces, types, member groups, and members.
When using deep API trees, render with a higher menu depth (for example `depth: 6`) so namespace/type/member levels are visible.

## Referencing API root from manual menus

You can reference the API root in `menu.yml`:

```yaml
home:
  - { path: readme.md, title: Home }
  - { path: api/readme.md, title: API Reference, folder: true }
```

When `folder: true`, Lunet reuses the generated API hierarchy as children of this menu item.
This makes API navigation work without creating a dedicated `api/menu.yml`.

## Data available in templates

Root API page (`api-dotnet`):

- `api.namespaces` -> namespace objects
- `api.objects` -> UID -> API object map
- `api.references` -> UID -> reference object map

Namespace page (`api-dotnet-namespace`) variable:

- `namespace` (`uid`, `name`, `summary`, `remarks`, `classes`, `structs`, `interfaces`, `enums`, `delegates`)

Member page (`api-dotnet-member`) variable:

- `member` (`uid`, `type`, `name`, `fullName`, `namespace`, `assemblies`, `summary`, `remarks`, `syntax`, `constructors`, `fields`, `properties`, `methods`, `events`, `operators`, `extensions`, `explicit_interface_implementation_methods`)

Global helper:

- `apiref "My.Namespace.Type"` -> API object by UID (or `null`)

## Linking with xref

Markdown:

```markdown
See [NuGetVersion](xref:NuGet.Versioning.NuGetVersion).
```

DocFX-style inline:

```markdown
<xref href="NuGet.Versioning.NuGetVersion"></xref>
```

Scriban:

```scriban
{{ '{{ $link = "NuGet.Versioning.NuGetVersion" | xref }}' }}
{{ '{{ if $link }}' }}<a href="{{ '{{ $link.url }}' }}">{{ '{{ $link.fullname }}' }}</a>{{ '{{ end }}' }}
```

## Referenced assemblies

Use `references` to include API from referenced assemblies (for example NuGet packages):

- `api.dotnet.references` applies to all configured projects
- `projects[i].references` applies only to one project
- both lists are merged (duplicates removed)

If XML docs are available for referenced assemblies, summaries and remarks are surfaced.

## Example in this repository

The Lunet docs site uses:

- `site/config.scriban` -> `api.dotnet` configuration
- `src/Lunet.ApiExample/Lunet.ApiExample.csproj` -> local sample project rendered under `/api`
- sample API includes multiple namespaces and broad modern C# surface (C# 9-14 style features: records, required/init members, primary constructors, checked operators, unsigned right shift, ref/scoped APIs, static abstract interface members, `allows ref struct`, extension members, native/function pointer signatures)
