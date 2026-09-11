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

## Download timeout

Set `resources.npm_request_timeout` in `config.scriban` to change the npm timeout, in **seconds**:

```scriban
# Allow up to five minutes for each registry request or complete tarball download.
resources.npm_request_timeout = 300

$bootstrap = resource "npm:bootstrap" "5.3.8"
```

Set this **before** calling `resource`, or before `extend` if the theme downloads resources during initialization. The default is `100`. Integer and fractional seconds are supported; the value must be a positive, finite number no greater than `2147483.647`. Invalid values produce an error when the timeout is read for a download rather than silently using the default.

The setting applies separately to each registry request and complete tarball download, not to local extraction. It shares its value with the .NET `NpmResourceProvider.RequestTimeout` property. `resources.npm` remains the collection of loaded npm resources.

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
- Registry requests and tarball downloads each have a 100-second timeout by default, including reading the complete response body. A timeout logs an error and returns `null`; it does not retry automatically. Adjust it with `resources.npm_request_timeout` as described above.
- Tarballs are downloaded to a temporary file before extraction. The temporary file is removed on completion or failure, and interrupted HTTP transfers do not leave partially extracted packages in the resource cache. Local extraction is not covered by the download timeout.
- If a resource is used in a bundle `js` or `css` call without a subpath and the package has no `main` entry, the build fails with an error.

## Troubleshooting downloads in CI

Lunet logs separate `NPM downloading` and `NPM extracting` messages so you can distinguish a network problem from archive processing. Download failures include the tarball URL and the HTTP status or timeout reason.

- An HTTP `429` response indicates rate limiting; `403` indicates denied access; `5xx` indicates a server-side failure. These responses fail the resource load rather than waiting indefinitely. Check registry availability and the runner's network/proxy configuration before retrying. A timeout alone does not prove rate limiting.
- Lunet downloads packages directly with .NET HTTP APIs; it does not run the npm CLI or read `.npmrc`. Changing npm's `fetch-timeout`, retry settings, or login credentials does not configure Lunet's downloader.
- Pin resource versions and persist the resource cache between CI builds to avoid unnecessary registry traffic. Resolving `latest` still requires a registry query when it has not already been loaded or cached under that name.

## See also

- [Bundles module](bundles.md) - using resources in CSS/JS bundles
- [SCSS module](scss.md) - using resource paths as SCSS include directories
