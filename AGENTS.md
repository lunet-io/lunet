# Lunet — Codex Agent Instructions

Lunet is a powerful static website generator built with .NET, powered by Scriban.

Paths/commands below are relative to this directory.

## Orientation

- **CLI entry point**: `src/Lunet/` — thin `Exe` (`PackAsTool`, command `lunet`), delegates to `Lunet.Application`.
- **App bootstrap**: `src/Lunet.Application/` — `LunetApp` registers all 24 plugin modules, creates `SiteApplication`, runs `SiteRunner`.
- **Core library**: `src/Lunet.Core/` — foundation: `SiteObject`, content pipeline, scripting, plugin base classes. All plugins depend on this. RootNamespace = `Lunet`.
- **Plugin libraries**: `src/Lunet.{PluginName}/` — each is a small self-contained library implementing one feature.
- **Tests**: `src/Lunet.Tests/` (NUnit 4.4, classic assert aliases). References Core, Yaml, Api.DotNet.Extractor.
- **Docs**: `readme.md` and `site/**/*.md` — keep in sync with behavior.
- **Default template**: `../templates/` (sibling checkout of <https://github.com/lunet-io/templates>). The `lunet init` skeleton in `src/Lunet.Core/shared/.lunet/new/site/` extends this template. Changes to the init skeleton may require coordinating with the templates repo.
- **Solution**: `src/lunet.slnx`. Central package management via `src/Directory.Packages.props`. All projects target `net10.0` (except `Lunet.Api.DotNet.Extractor` -> `netstandard2.0`).

## Architecture

**Must read**: [`site/architecture.md`](site/architecture.md) — dependency graph, plugin system, class hierarchy, content pipeline, virtual file system, Scriban templating, and `shared/` folder conventions.

## Build & Test

```sh
# from the project root (this folder)
cd src
dotnet build -c Release
dotnet test -c Release

# build the website with the local lunet build (dogfooding)
cd ../site
dotnet ../src/Lunet/bin/Release/net10.0/lunet.dll --stacktrace build --dev
```

All tests must pass and docs must be updated before submitting. Do not use a globally installed `lunet` to validate `site/` in this repository.

## Contribution Rules (Do/Don't)

- Keep diffs focused; avoid drive-by refactors/formatting and unnecessary dependencies.
- Follow existing patterns and naming; prefer clarity over cleverness.
- New/changed behavior requires tests; bug fix = regression test first, then fix.
- All public APIs require XML docs (avoid CS1591) and should document thrown exceptions.

## C# Conventions (Project Defaults)

- Naming: `PascalCase` public/types/namespaces, `camelCase` locals/params, `_camelCase` private fields, `I*` interfaces.
- Style: file-scoped namespaces; `using` outside namespace (`System` first); `var` when the type is obvious.
- Nullability: enabled — respect annotations; use `ArgumentNullException.ThrowIfNull()`; prefer `is null`/`is not null`; don't suppress warnings without a justification comment.
- Exceptions: validate inputs early; throw specific exceptions (e.g., `ArgumentException`/`ArgumentNullException`) with meaningful messages.
- Async: `Async` suffix; no `async void` (except event handlers); use `ConfigureAwait(false)` in library code; consider `ValueTask<T>` on hot paths.

## Performance / AOT / Trimming

- Minimize allocations (`Span<T>`, `stackalloc`, `ArrayPool<T>`, `StringBuilder` in loops).
- Keep code AOT/trimmer-friendly: avoid reflection; prefer source generators; use `[DynamicallyAccessedMembers]` when reflection is unavoidable.
- Use `sealed` for non-inheritable classes; prefer `ReadOnlySpan<char>` for parsing.

## API Design

- Follow .NET guidelines; keep APIs small and hard to misuse.
- Prefer overloads over optional parameters (binary compatibility); consider `Try*` methods alongside throwing versions.
- Mark APIs `[Obsolete("message", error: false)]` before removal once stable (can be skipped while pre-release).

## Git / Pre-Submit

- Commits: commit after each self-contained logical step; imperative subject, < 72 chars; one logical change per commit; reference issues when relevant; don't delete unrelated local files.
- Checklist: each self-contained step is committed; build+tests pass; docs updated if behavior changed; public APIs have XML docs; changes covered by unit tests.
