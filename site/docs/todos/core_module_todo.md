---
title: "TODO: Core module"
discard: true
---

# TODO: Core module

- Add unit tests for content pipeline behavior using `Zio.MemoryFileSystem` (pages vs static files, includes/excludes, URL mapping).
- Improve error reporting for common misconfigurations (missing layouts/includes, invalid `baseurl`, invalid `basepath`).
- Review thread-safety assumptions in content processors (many use shared state + locks).
- Audit for nullable reference types end-to-end; list dependencies that are not annotated (Zio, Scriban, Markdig, Lunr, NUglify, DotNet.Globbing).

