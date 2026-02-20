---
title: "Markdown module"
---

# Markdown module

The Markdown module converts Markdown pages to HTML using [Markdig](https://github.com/xoofx/markdig) and integrates with the Lunet layout system. It also provides a `markdown.to_html` helper for use in Scriban templates.

## Configuration vs template usage

{.table}
| Where | What you do |
|---|---|
| `config.scriban` | Configure pipeline options (`markdown.options.*`) |
| Markdown pages (`.md`, `.markdown`, `.sbn-md`) | Write content and front matter |
| Scriban templates/layouts | Render markdown with `markdown.to_html(...)` when needed |

## Markdown page flow

1. A Markdown file is loaded as a page (front matter extracted).
2. The layout processor detects `content_type = markdown`.
3. The Markdown converter renders the content to HTML.
4. Content type changes to `html`.
5. The HTML layout is resolved and applied.

> **Important:** Markdown conversion only happens when a layout is found for the page. Markdown files without a matching layout are **not** converted to HTML and pass through as raw Markdown.

Minimal page:

```markdown
---
title: "Hello"
---

# Hello
This is **Markdown**.
```

## Configuration

> All `markdown.*` properties shown on this page are set inside your site's `config.scriban`.

```scriban
with markdown
  options.extensions = "advanced"
  options.css_img_attr = "img-fluid,rounded"
  options.auto_id_kind = "github" # or "ascii"
end
```

{.table}
| Option | Default | Description |
|---|---|---|
| `markdown.options.extensions` | `"advanced"` | Markdig pipeline preset. Currently only `"advanced"` is supported; any other value also defaults to advanced mode |
| `markdown.options.css_img_attr` | *(empty)* | Comma-separated CSS classes added to all generated `<img>` elements |
| `markdown.options.auto_id_kind` | `"github"` | Heading auto-ID mode. Supported values: `"github"` (GFM style, keeps Unicode letters) or `"ascii"` (ASCII/transliterated IDs) |

### Heading auto-ID modes

Lunet enables Markdig auto-identifiers for headings. You can choose how IDs are generated:

- `github` (default): GitHub-compatible behavior. Keeps Unicode letters and lowercases text.
- `ascii`: Converts to ASCII-friendly IDs by stripping accents and non-ASCII characters.

Example heading:

```markdown
# Über Åß
```

Generated IDs:

{.table}
| `auto_id_kind` | Generated heading ID |
|---|---|
| `github` | `über-åß` |
| `ascii` | `uber-ass` |

## Supported Markdown features

Lunet uses Markdig's `UseAdvancedExtensions()` and then customizes alert and xref behavior.

{.table}
| Feature group | Status | Notes |
|---|---|---|
| Standard CommonMark | enabled | Headings, lists, links, code fences, blockquotes, etc. |
| Abbreviations | enabled | Abbreviation definitions |
| Auto identifiers | enabled | Automatic heading IDs |
| Auto links | enabled | Bare URL and email auto-linking |
| Citations | enabled | `"..."` citation syntax |
| Custom containers | enabled | `:::` container blocks |
| Definition lists | enabled | `Term` + `: definition` |
| Diagrams | enabled | Mermaid, nomnoml diagram blocks |
| Emphasis extras | enabled | Strikethrough, superscript, subscript, inserted, marked |
| Figures | enabled | Figure/figcaption syntax |
| Footers and footnotes | enabled | Footer and footnote syntax |
| Generic attributes | enabled | `{#id .class attr=val}` inline attributes |
| List extras / task lists | enabled | Extra bullet styles (alpha, roman) and `[x]` checkboxes |
| Math | enabled | `$...$` inline and `$$...$$` block math |
| Media links | enabled | Media-aware links (YouTube, etc.) |
| Pipe/grid tables | enabled | Both table syntaxes |
| Alerts / callouts | enabled | Markdig alert parser with custom Lunet HTML renderer |
| `xref:` links | enabled | Custom Lunet extension for cross-reference resolution |

Features **not** enabled: Emoji, SmartyPants, Bootstrap styling, soft-line-to-hard-line conversion.

## Callout blocks

Markdown alert syntax is supported:

```markdown
> [!NOTE]
> This is a note.
```

Lunet renders alerts with a custom HTML structure using CSS class hooks:

```html
<div class="lunet-alert-note">
  <div class='lunet-alert-note-heading'>
    <span class='lunet-alert-note-icon'></span>
    <span class='lunet-alert-note-heading-text'></span>
  </div>
  <div class='lunet-alert-note-content'>
    ...content...
  </div>
</div>
```

Common kinds: `note`, `tip`, `important`, `warning`, `caution`. The kind is lowercased in the class names.

## XRef links

You can link to a UID known by Lunet using xref syntax:

```markdown
See <xref:My.Namespace.MyType>.
```

DocFX-style inline tags are also recognized:

```markdown
<xref href="My.Namespace.MyType"></xref>
```

During page conversion, Lunet resolves `xref:` using the site UID map, looks up the display title, and rewrites the link to the final URL. If the UID is not found, the raw UID text is used as the link label.

Relative links in Markdown content are also resolved through Lunet's URL reference system during page conversion.

## `markdown.to_html` helper

The `markdown.to_html(text)` function is available in all Scriban templates:

```scriban
result = markdown.to_html "# Hello"
```

This helper runs Markdig on the provided text but does **not** perform page-aware processing — specifically:

- No relative URL rewriting
- No `xref:` link resolution
- No `css_img_attr` class injection

For full link resolution, use Markdown content pages instead.

## See also

- [Layouts and includes](/docs/layouts-and-includes/) — how layouts wrap converted Markdown
- [Content and front matter](/docs/content-and-frontmatter/) — front matter in Markdown pages
- [SCSS module](scss.md) — for styling alert blocks and Markdown output
