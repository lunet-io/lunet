---
title: "Markdown module"
---

# Markdown module

The Markdown module converts Markdown pages to HTML and plugs into Lunet layouts.

## Configuration vs template usage

{.table}
| Where | What you do |
|---|---|
| `config.scriban` | configure pipeline options (`markdown.options.*`) |
| Markdown pages (`.md`, `.markdown`, `.sbn-md`) | write content and front matter |
| Scriban templates/layouts | render markdown with `markdown.to_html(...)` when needed |

## Markdown page flow

1. File is loaded as a page (front matter or script template).
2. Layout processor sees `content_type = markdown`.
3. Markdown converter renders HTML.
4. Content type becomes `html`.
5. HTML layout is resolved and applied.

Minimal page:

```markdown
---
title: "Hello"
---

# Hello
This is **Markdown**.
```

## Configuration (`config.scriban`)

```scriban
with markdown
  options.extensions = "advanced"
  options.css_img_attr = "img-fluid,rounded"
end
```

{.table}
| Option | Default | Description |
|---|---|---|
| `markdown.options.extensions` | `"advanced"` | Markdig pipeline preset. Lunet currently uses advanced mode. |
| `markdown.options.css_img_attr` | empty | Comma-separated CSS classes added to generated `<img>` elements. |

## Supported Markdown features

Lunet uses Markdig `UseAdvancedExtensions()` and then customizes alerts/xref behavior.

{.table}
| Feature group | Status in Lunet | Notes |
|---|---|---|
| Standard CommonMark | enabled | headings, lists, links, code fences, blockquotes, etc. |
| Abbreviations | enabled | Markdig advanced extension |
| Auto identifiers | enabled | heading ids |
| Auto links | enabled | bare URLs/mail links |
| Citations | enabled | advanced extension |
| Custom containers | enabled | `:::` blocks |
| Definition lists | enabled | `Term` + `: definition` |
| Diagrams | enabled | advanced extension |
| Emphasis extras | enabled | extra emphasis syntax |
| Figures | enabled | figure/caption support |
| Footers and footnotes | enabled | advanced extensions |
| Generic attributes | enabled | `{#id .class}` attributes |
| List extras / task lists | enabled | extra list styles and checkboxes |
| Math | enabled | math syntax support |
| Media links | enabled | media-aware links |
| Pipe/grid tables | enabled | both table syntaxes |
| Alerts / callouts | enabled (custom renderer) | Markdig alert parser + Lunet HTML/CSS classes |
| `xref:` links | enabled (Lunet extension) | supports `<xref:uid>` resolution |

Markdig features explicitly not enabled by `UseAdvancedExtensions()` remain off by default (for example Emoji, SmartyPants, Bootstrap styling, soft-line-to-hard-line conversion).

## Callout blocks (`[!NOTE]`, `[!WARNING]`, …)

Markdown alert syntax is supported:

```markdown
> [!NOTE]
> This is a note.
```

Lunet renders alerts with classes:
- `lunet-alert-<kind>`
- `lunet-alert-<kind>-heading`
- `lunet-alert-<kind>-content`

Common kinds are `NOTE`, `TIP`, `IMPORTANT`, `WARNING`, and `CAUTION`.

## XRef links in markdown

You can link to a UID known by Lunet:

```markdown
See <xref:My.Namespace.MyType>.
```

DocFX-style inline tags are also recognized:

```markdown
<xref href="My.Namespace.MyType"></xref>
```

During page conversion, Lunet resolves `xref:` using the site UID map and rewrites links to final URLs.

## `markdown.to_html(...)` helper

`markdown.to_html(string)` is available in templates:

```scriban
{{ '{{ markdown.to_html "# Hello" }}' }}
```

This helper runs Markdig on the provided text, but it does not perform page-aware URL rewriting (`ref`/`relref` context) done during normal page conversion.
