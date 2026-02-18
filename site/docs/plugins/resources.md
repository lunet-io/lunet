---
title: "Resources module (npm)"
---

# Resources module (npm)

The resources module downloads and caches external packages for use in your site. The built-in provider is `npm`, which fetches packages from the npm registry.

Resources are typically loaded in `config.scriban` and used with the [Bundles module](bundles.md).

## Load an npm package

```scriban
$bootstrap = resource "npm:bootstrap" "5.3.8"
```

The string argument format is `"provider:package"`. If you omit the version, it defaults to `"latest"` (the highest non-prerelease version):

```scriban
$bootstrap = resource "npm:bootstrap"
```

## Returned object

The `resource` function returns an object with the following properties accessible in Scriban:

{.table}
| Property | Description |
|---|---|
| `path` | Virtual path to the package folder (e.g. `/resources/npm/bootstrap/5.3.8`) |
| `provider` | Provider name (e.g. `"npm"`) |
| `main` | Absolute path to the package's main entry point (from `package.json`), if present |

For npm packages, all fields from `package.json` are also available (e.g. `description`, `license`, `homepage`, `style`, `sass`).

## Using resources with bundles

The most common use of resources is adding files to a bundle. Pass the resource handle and a subpath within the package:

```scriban
with bundle
  $bootstrap = resource "npm:bootstrap" "5.3.8"

  css $bootstrap "/dist/css/bootstrap.min.css"
  js $bootstrap "/dist/js/bootstrap.bundle.min.js"
end
```

If you omit the subpath, the package's `main` field from `package.json` is used:

```scriban
with bundle
  $tocbot = resource "npm:tocbot" "4.36.4"
  js $tocbot    # uses tocbot's "main" entry
end
```

You can also use a resource's `path` property to configure SCSS include paths:

```scriban
with bundle
  $bootstrap = resource "npm:bootstrap" "5.3.8"
  scss.includes.add $bootstrap.path + "/scss"
end
```

## Version resolution

When the version is `"latest"`, Lunet queries the npm registry and selects the highest non-prerelease semver version. If that version is already cached locally, no download occurs.

When an exact version is specified (e.g. `"5.3.8"`), it is used directly.

## Caching

Resources are downloaded once and cached locally. Subsequent builds reuse the cached copy.

By default, resources are stored in the private build cache (`.lunet/build/cache/.lunet/resources/npm/<package>/<version>/`). This directory is typically excluded from version control.

## Advanced: object-form query

For more control, pass a Scriban object instead of a string:

```scriban
$pkg = resource { provider: "npm", name: "bootstrap", version: "5.3.8", public: true }
```

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `provider` | string | (required) | Provider name (e.g. `"npm"`) |
| `name` | string | (required) | Package name |
| `version` | string | `"latest"` | Exact version or `"latest"` |
| `public` | bool | `false` | Store in the public `.lunet/` directory instead of the build cache |
| `pre_release` | bool | `false` | Allow pre-release versions when resolving `"latest"` |

## Error handling

- If the package or version is not found on the npm registry, the build logs an error and returns `null`.
- If the registry is unreachable, the build logs an error. Previously cached packages remain available.
- If a resource is used in a bundle `js` or `css` call without a subpath and the package has no `main` entry, the build fails with an error.

## See also

- [Bundles module](bundles.md) — using resources in CSS/JS bundles
- [SCSS module](scss.md) — using resource paths as SCSS include directories
