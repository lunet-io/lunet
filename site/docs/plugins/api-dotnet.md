---
title: "API module (.NET)"
---

# API module (.NET)

Lunet can generate API documentation for .NET projects/assemblies using the `api.dotnet` integration.

## Enable in config

```scriban
$api = api.dotnet
$api.title = "MyProject API"
$api.projects = [
  { name: "MyProject", path: "../src/MyProject/MyProject.csproj" }
]
```

This generates dynamic pages under `/api/**` and registers xref targets for linking from Markdown.

## Customize

```scriban
$api.layout = "_default"
$api.include_helper = "_builtins/api-dotnet-helpers.sbn-html"
```

