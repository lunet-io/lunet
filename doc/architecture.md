# Lunet Architecture (Agent Cheat Sheet)

Compact reference meant for AI agents working in this repo. Keep diffs focused; see `AGENTS.md` for contribution rules and build/test commands.

## Read-First Entry Points (Open in This Order)

- CLI entry: `src/Lunet/Program.cs` (parses `--profiler`, creates `SiteConfiguration`, runs `LunetApp`)
- Module registration / plugin order: `src/Lunet.Application/LunetApp.cs` (`Modules` list order matters)
- CLI parsing + command runners: `src/Lunet.Core/Core/SiteApplication.cs` (`init`, `clean`; other commands are added by modules)
- Main run loop / reloads: `src/Lunet.Core/Core/SiteRunner.cs` (loops while a command runner returns `Continue`)
- Site model + initialization: `src/Lunet.Core/Core/SiteObject.cs` (plugins, config load, `Build()`)
- Content pipeline: `src/Lunet.Core/Core/ContentPlugin.cs` (scan → run templates → process → write output)
- Scripting glue: `src/Lunet.Core/Core/ScriptingPlugin.cs` (front matter, page eval, include loader rules)
- Layout resolution/conversion: `src/Lunet.Layouts/LayoutProcessor.cs` (layouts + converters)

## Runtime Flow (CLI → Output)

```
lunet <command> [options]
  └─ LunetApp.Run(...)                          (src/Lunet.Application/LunetApp.cs)
       ├─ SiteApplication.Execute(args)         (registers ISiteCommandRunner instances)
       └─ SiteRunner.Run()
            ├─ CurrentSite = new SiteObject(Config)
            ├─ foreach CommandRunner: runner.Run(...)
            ├─ if Continue: CurrentSite = CurrentSite.Clone()  (new SiteObject, new plugins)
            └─ repeat until Exit/ExitWithError
```

Commands (as of this repo state):
- `lunet init [folder]` (Core) → copies skeleton from shared meta `/new/site`.
- `lunet clean` (Core) → deletes `.lunet/build` in the input site.
- `lunet build [--watch] [--dev] [--no-threads]` (Watcher module) → builds once or rebuilds on changes.
- `lunet serve [--no-watch] [--no-threads]` (Server module) → builds + runs web server + (optional) watches.

## Solution/Dependency Map (High Level)

```
Lunet (CLI exe; tool command `lunet`)
  └─ Lunet.Application (registers all modules/plugins)
       └─ Lunet.Core (foundation: Zio VFS, Scriban, Autofac, pipeline types)
            ↑ every plugin depends on Core

Notable plugin-to-plugin deps (constructor injection):
  Datas ← { Json, Toml, Yaml }                         (data loaders; YAML also adds front matter parser)
  Layouts ← { Markdown, Rss, Sass, Taxonomies, ... }   (layout processor + converters)
  Watcher ← Server                                     (ServeCommandRunner derives from BuildCommandRunner)
  Api ← Api.DotNet (← Api.DotNet.Extractor netstandard2.0)
  Extends adds content FS layers (themes)
```

## Plugin System (Autofac DI)

Terminology: **Module** wires CLI + registers **Plugin** type; **Plugin** mutates `SiteObject` (registers processors, loaders, builtins, etc.).

- `SiteModule<TPlugin>` (Core) automatically calls `application.Config.RegisterPlugin<TPlugin>()`.
- Plugins are instantiated in `SiteObject.InitializePlugins()` (Autofac container per `SiteObject`).
- Plugin constructors run before `config.scriban` is imported. Use defaults + let config override via site variables.
- Ordering:
  - Module order is `LunetApp.Modules`.
  - Processor priority inside `OrderedList<T>` is “last added runs first” (see Content processing below).

Typical pattern (one file per plugin project):
```csharp
public sealed class FooModule : SiteModule<FooPlugin> { }

public sealed class FooPlugin : SitePlugin
{
    public FooPlugin(SiteObject site, BarPlugin bar) : base(site)
    {
        // register processors, converters, parsers, builtins, ...
    }
}
```

## Virtual File System Model (Zio)

All paths below are *virtual* `UPath` rooted at `/`.

- `SharedFileSystem` (read-only): physical folder `AppContext.BaseDirectory/shared`.
  - This is produced by MSBuild copying each project’s `shared/**` into the app output.
- `FileSystem` (aggregate): `ContentFileSystems...` + `InputFileSystem` + `SharedFileSystem`.
  - Content FS layers are added via `Site.AddContentFileSystem(...)` (e.g. themes via Extends).
- `MetaFileSystem` (aggregate view of `/.lunet/`):
  - Highest priority: `CacheMetaFileSystem` → `<input>/.lunet/build/cache/.lunet/` (if input FS is set)
  - Then: `SubFileSystem(FileSystem, "/.lunet")` (user + content FS layers + shared)
- `OutputFileSystem` (physical): default `<input>/.lunet/build/www/` (overridable via `-o|--output-dir`)

Key folders in `MetaFileSystem` (conventions, not magic):
- `/.lunet/includes/**` → `include` templates (Scriban)
- `/.lunet/layouts/**` → layout templates
- `/.lunet/data/**` → data files loaded into `site.data` (before config)
- `/.lunet/extends/**` → themes/extensions (also used as content FS layers)
- `/.lunet/modules/**` → bundled modules (used by some plugins, e.g. Api.DotNet)

## Config + Scripting (Scriban)

- Site config file is `/<site>/config.scriban` (`SiteFileSystems.DefaultConfigFileName`).
- Loaded once per `SiteObject` in `SiteObject.Initialize()` using `ScriptFlags.AllowSiteFunctions`.
  - This pushes `site.builtins` into the scripting globals.
  - **Includes are intentionally disabled** in this mode (see `TemplateLoaderUnauthorized` in `ScriptingPlugin`).
- CLI `--define name=value` is executed as a Scriban statement against the `SiteObject` (see `SiteObject.AddDefine`).
- Page + layout templates are Scriban templates; they *can* use `include` from `/.lunet/includes/**`.

Builtins worth knowing (registered during plugin init):
- `site.builtins.lunet.version` (Core)
- `site.builtins.defer <expr>` (Core) → late string replacement after processing
- `site.builtins.xref(uid)`, `ref(url)`, `relref(url)` (Core `PageFinderProcessor`)
- `site.builtins.markdown.to_html(str)` (Markdown plugin)

## Content Model (Files → Pages → Output)

Classification happens in `ContentPlugin.LoadAllContent()`:
- Enumerates `Site.FileSystem` and keeps only paths where `site.IsHandlingPath(path)` is true.
- Default path rules (`SiteObject`):
  - Force excludes: `**/.lunet/build/**`, `/config.scriban`
  - Excludes (unless explicitly included): `**/~*/**`, `**/.*/**`, `**/_*/**`
  - Includes: `**/.lunet/**` (overrides excludes)
- Detects front matter by asking `site.Scripts.FrontMatterParsers` if the first bytes match.
  - With front matter → `Site.Pages` (template-backed)
  - Without front matter → `Site.StaticFiles` (copied as-is unless a converter applies)
- Each file becomes a `FileContentObject`:
  - `Section` = first directory segment (used as default `Layout`)
  - `Layout` defaults to `Section` (unless pre-content sets it)
  - Filename `YYYY-MM-DD-title.ext` sets `Date`, `Slug`, `Title` (legacy blog convention)

URL rules (in `ContentObject.Initialize()` and `GetDestinationPath()`):
- If HTML-like and `site.UrlAsFile == false`, pages default to “folder URLs” (`/x/`), written as `/x/index.html`.
- `index.*` and (if `site.ReadmeAsIndex`) `readme.*` map to the parent folder URL (`/section/`).
- `site.BasePath` is prepended to `Url` but not to `UrlWithoutBasePath`.

## Content Pipeline Hooks (ContentPlugin)

`ContentPlugin` owns hook lists; plugins register into these lists (usually in their constructors):

- `BeforeInitializingProcessors : OrderedList<ISiteProcessor>` → `ProcessingStage.BeforeInitializing`
- `BeforeLoadingProcessors : OrderedList<ISiteProcessor>` → `ProcessingStage.BeforeLoadingContent`
- `BeforeLoadingContentProcessors : OrderedList<TryProcessPreContentDelegate>` → per-path, can create `preContent` ScriptObject
- `AfterLoadingProcessors : OrderedList<ISiteProcessor>` → `ProcessingStage.AfterLoadingContent` (includes `PageFinderProcessor`)
- `AfterRunningProcessors : OrderedList<IContentProcessor>` → `ContentProcessingStage.Running`
- `BeforeProcessingProcessors : OrderedList<ISiteProcessor>` → `ProcessingStage.BeforeProcessingContent`
- `ContentProcessors : OrderedList<IContentProcessor>` → `ContentProcessingStage.Processing`
- `AfterProcessingProcessors : OrderedList<ISiteProcessor>` → `ProcessingStage.AfterProcessingContent`

Processing details that matter for agent work:
- `IContentProcessor` priority is **reverse list order** (loop runs from `Count-1` down to `0`).
- A processor can only “hit” a page once per stage (it is removed from the pending list after returning a non-`None` result).
- `ContentResult.Break` stops further processing **and suppresses output copy** for that page (used when a processor “owns” output generation).
- After processing (and before writing), `page.ApplyDefer()` runs (deferred string substitutions).
- Stale outputs are deleted at the end of a build (`CleanupOutputFiles()`).

## Layouts + Converters (Layouts plugin)

`LayoutPlugin` registers a `LayoutProcessor` into `site.Content.ContentProcessors`.

- Layouts live under `/.lunet/layouts/` in `MetaFileSystem`.
- Default layout name is `_default`.
- Lookup key = `(layoutName, layoutType, contentType)`.
- Layout search tries several paths (single/list) and tries all extensions registered for `contentType`.
- If no layout exists, `LayoutProcessor` may still convert content via `ILayoutConverter`:
  - `forLayout == true` → converter runs if registered.
  - `forLayout == false` → converter runs only if `converter.ShouldConvertIfNoLayout == true` (e.g. scss→css).

Layout-time conventions:
- Layout evaluation exposes a `content` variable and merges layout front matter/script locals into the page locals.
- For `layout_type == "list"`, layout processing sets `page.pages = site.pages` (used for list rendering).

## Themes / Extends (Extends plugin)

Themes/extensions are layered file systems.

- `extend(...)` supports local names (`/.lunet/extends/<name>`) and direct GitHub references (`owner/repo` or `owner/repo@tag`, default directory `dist`), then imports the extension `config.scriban` if present.
- The theme FS is added via `Site.AddContentFileSystem(themeFs)`, so it can override user/site content.

## “Where Do I Add X?” (Fast Recipes)

- New CLI command: add a `SiteModule`/`SiteModule<TPlugin>` and call `application.Command(...)` (see `src/Lunet.Watcher/SiteWatcherService.cs`, `src/Lunet.Server/ServerModule.cs`).
- New pipeline stage hook: pick the right `OrderedList` on `site.Content` and add an `ISiteProcessor` or `IContentProcessor`.
- New file type conversion: implement `ILayoutConverter` and register via `layoutPlugin.Processor.RegisterConverter(...)`.
- New front matter syntax: implement `IFrontMatterParser` and add to `site.Scripts.FrontMatterParsers`.
- New built-in templates/includes/layouts: add files under `src/<Project>/shared/.lunet/...` and ensure the project copies `shared/**` to output.
- Persist state across `--watch`: plugin instances are recreated on reload; store reusable state in `SiteConfiguration.SharedCache` or an `ISiteService` registered in `SiteRunner`.

## Naming Surprises

- Folder `src/Lunet.Markdig/` builds assembly `Lunet.Markdown` with namespace `Lunet.Markdown`.
- Folder `src/Lunet.NUglify/` builds assembly `Lunet.Minifiers` with namespace `Lunet.Minifiers`.
