---
title: "API module"
---

# API module

The API module provides a root `api` object used to register API generators.

In practice, you typically use:

- [API module (.NET)](api-dotnet.md)

The `.NET` API generator integrates with xref and menu infrastructure automatically (generated pages + generated menu tree).

From config:

```scriban
$dotnet = api.dotnet
```

The `.dotnet` property is provided by the `.NET API` module and is created lazily (only if you access it).
