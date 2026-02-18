---
title: "API module"
---

# API module

The API module provides the `site.api` root object used to register API documentation generators. It acts as a registry — language-specific generators register themselves as named sub-objects.

Currently available generators:

- [API module (.NET)](api-dotnet.md) — generates .NET API reference documentation from `.csproj` files

## Usage

API generators are accessed as properties on `site.api`. They are created lazily — configuration objects are only instantiated when first accessed from `config.scriban`:

```scriban
# Accessing api.dotnet creates the .NET API config object
with api.dotnet
  title = "MyProject API Reference"
  projects = [{ name: "MyProject", path: "../src/MyProject/MyProject.csproj" }]
end
```

## See also

- [API module (.NET)](api-dotnet.md) — .NET API documentation generator
- [Markdown module](markdown.md) — `xref:` link resolution for cross-referencing API members
