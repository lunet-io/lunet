---
title: "Resources module (npm)"
---

# Resources module (npm)

The resources module downloads and caches external assets. Today, the built-in provider is `npm`.

## Load an npm package

```scriban
$bootstrap = resource "npm:bootstrap" "5.3.8"
```

If you omit the version, it defaults to `latest`:

```scriban
$bootstrap = resource "npm:bootstrap"
```

The returned object exposes:
- `path` — package folder in the Lunet cache
- `provider` — provider name (`npm`)
- provider-specific fields (for example a `main` entry if present)

## Public vs private caching

Resource installations default to “private” (stored under the build cache). You can opt into public storage:

```scriban
$pkg = resource { provider: "npm", name: "bootstrap", version: "5.3.8", public: true }
```

## Pre-releases

To allow pre-release versions when using `latest`:

```scriban
$pkg = resource { provider: "npm", name: "somepkg", version: "latest", pre_release: true }
```
