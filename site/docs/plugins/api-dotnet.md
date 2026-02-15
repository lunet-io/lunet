---
title: "API module (.NET)"
---

# API module (.NET)

Lunet can generate API documentation for .NET projects and their referenced assemblies with `api.dotnet`.

## Configuration vs template usage

{.table}
| Where | What you do |
|---|---|
| `config.scriban` | configure projects, build properties, referenced assemblies |
| `site/.lunet/layouts/*.api-dotnet*.sbn-md` | customize API pages (root, namespace, member) |
| Markdown/content pages | reference API UIDs with `xref:` links |
| Scriban templates | query API objects with `apiref`/`xref` |

## Quick start (`config.scriban`)

```scriban
with api.dotnet
  title = "MyProject API Reference"
  config = "Release"
  properties = {
    TargetFramework: "net10.0",
    GenerateDocumentationFile: true
  }
  projects = [
    {
      name: "MyProject",
      path: "../src/MyProject/MyProject.csproj",
      references: ["NuGet.Versioning"] # optional per-project referenced assemblies
    }
  ]
  references = ["MyCompany.Shared"] # optional global referenced assemblies
end
```

This generates dynamic pages under `/api/**` and registers xref targets for linking from Markdown.
Using `with api.dotnet` is the idiomatic style in Lunet configs because it keeps the config block concise.

## Configuration reference

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `api.dotnet.title` | string | `"<site title> .NET API Reference"` | Title for `/api/` root page |
| `api.dotnet.config` | string | `"Release"` | Build configuration passed to `dotnet build` |
| `api.dotnet.properties` | object | empty | Extra MSBuild properties (`-p:`), e.g. `TargetFramework` |
| `api.dotnet.projects` | array | required | Projects to extract (`string` or object entries) |
| `api.dotnet.references` | array<string> | empty | Global referenced assemblies to include |
| `api.dotnet.include_helper` | string | `"_builtins/api-dotnet-helpers.sbn-html"` | Helper include used by default API layouts |
| `api.dotnet.layout` | string | `"_default"` | Base layout used by generated API pages |

Project entry object:

{.table}
| Key | Type | Required | Description |
|---|---|---|---|
| `name` | string | no | Logical project name (cache/output id). Defaults to project filename |
| `path` | string | yes | Path to `.csproj` (or glob pattern) |
| `properties` | object | no | Per-project MSBuild properties (overrides global `properties`) |
| `references` | array<string> / string | no | Per-project referenced assemblies to include |

## Generated pages and layout types

Lunet creates dynamic API pages:

- `/api/` with `layout_type = "api-dotnet"` (API root)
- `/api/<namespace-uid>/` with `layout_type = "api-dotnet-namespace"`
- `/api/<member-uid>/` with `layout_type = "api-dotnet-member"`

Default templates live in:

- `src/Lunet.Api.DotNet/shared/.lunet/layouts/_default.api-dotnet.sbn-md`
- `src/Lunet.Api.DotNet/shared/.lunet/layouts/_default.api-dotnet-namespace.sbn-md`
- `src/Lunet.Api.DotNet/shared/.lunet/layouts/_default.api-dotnet-member.sbn-md`
- `src/Lunet.Api.DotNet/shared/.lunet/includes/_builtins/api-dotnet-helpers.sbn-html`

Override them by creating files with the same names under `site/.lunet/layouts` or `site/.lunet/includes`.

## Data available in templates

Root API page (`api-dotnet`):

- `api.namespaces` -> namespace objects
- `api.objects` -> UID -> API object map
- `api.references` -> UID -> reference object map

Namespace page (`api-dotnet-namespace`) variable:

- `namespace` (keys: `uid`, `name`, `summary`, `remarks`, `classes`, `structs`, `interfaces`, `enums`, `delegates`)

Member page (`api-dotnet-member`) variable:

- `member` (keys: `uid`, `type`, `name`, `fullName`, `namespace`, `assemblies`, `summary`, `remarks`, `syntax`, `constructors`, `fields`, `properties`, `methods`, `events`, `operators`, `extensions`, `explicit_interface_implementation_methods`)

Global helper available in templates:

- `apiref "My.Namespace.Type"` -> returns API object by UID (or `null` if not found)

## Render namespace/type menus

Example custom API root layout:

```scriban
## Namespaces
{{ '{{ for ns in api.namespaces }}' }}
{{ '{{ $xref = ns.uid | xref }}' }}
{{ '{{ if $xref }}' }}
- [{{ '{{ $xref.name }}' }}]({{ '{{ $xref.url }}' }})
{{ '{{ end }}' }}
{{ '{{ end }}' }}
```

Example namespace page section:

```scriban
## Classes
{{ '{{ for type in namespace.classes }}' }}
- {{ '{{ type.uid | xref_to_html_link }}' }}
{{ '{{ end }}' }}
```

`xref_to_html_link` is defined by `_builtins/api-dotnet-helpers.sbn-html` (or your own include helper).

## Link to types/members with xref

Markdown:

```markdown
See [NuGetVersion](xref:NuGet.Versioning.NuGetVersion).
```

DocFX-style inline tag:

```markdown
<xref href="NuGet.Versioning.NuGetVersion"></xref>
```

Scriban:

```scriban
{{ '{{ $link = "NuGet.Versioning.NuGetVersion" | xref }}' }}
{{ '{{ if $link }}' }}<a href="{{ '{{ $link.url }}' }}">{{ '{{ $link.fullname }}' }}</a>{{ '{{ end }}' }}
```

Use full member UIDs for methods/operators/properties (for example with parameter signatures).

## Referenced assemblies (`references`)

When `references` are provided, Lunet includes matching compilation references (for example package references) in the generated API model.

Use simple assembly names (`NuGet.Versioning`) or filenames (`NuGet.Versioning.dll`).

- `api.dotnet.references` applies to all configured projects
- `projects[i].references` applies only to that project
- both lists are merged (duplicates removed)

If XML docs are available for referenced assemblies, summaries/remarks are surfaced in generated pages.
