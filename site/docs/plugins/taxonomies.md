---
title: "Taxonomies module"
---

# Taxonomies module

Taxonomies provide many-to-many grouping for pages — tags, categories, or any custom classification you define. The module scans page front matter, collects terms, and generates listing pages automatically.

## Default taxonomies

Two taxonomies are pre-defined:

{.table}
| Name | Singular | Default URL |
|---|---|---|
| `tags` | `tag` | `/tags/` |
| `categories` | `category` | `/categories/` |

You can use these immediately without any configuration.

## Define taxonomies

Add or override taxonomy definitions in your `config.scriban`:

> All `taxonomies.*` properties shown on this page are set inside your site's `config.scriban`, within a `with taxonomies` / `end` block.

```scriban
with taxonomies
  # Simple form: name = "singular"
  tags = "tag"
  categories = "category"

  # Object form: name = { singular, url, map }
  series = { singular: "serie", url: "/series/" }
end
```

### Simple form

```scriban
tags = "tag"
```

Sets the singular display name. The URL defaults to `/{name}/`.

### Object form

```scriban
series = { singular: "serie", url: "/series/", map: { "C#": "csharp" } }
```

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `singular` | string | *(required)* | Singular display name of the taxonomy |
| `url` | string | `"/{name}/"` | Base URL path for the taxonomy |
| `map` | object | `{}` | Term-name-to-slug overrides for URL generation |

The `map` property lets you control how specific term names are slugified in URLs. For example, `map: { "C#": "csharp" }` produces `/tags/csharp/` instead of the default URL-encoding of "C#". Without a map entry, term names are slugified via Markdig's `Urilize` function (non-ASCII characters are preserved).

### Removing default taxonomies

To remove the built-in `tags` and `categories` definitions, call `clear` before defining your own:

```scriban
with taxonomies
  taxonomies.clear
  series = "serie"
end
```

## Tag a page

Assign terms to pages in front matter. The value must be an array of strings:

```yaml
tags: ["lunet", "docs"]
categories: ["guides"]
series: ["getting-started"]
```

## Generated pages

For each taxonomy that has at least one term, Lunet generates:

{.table}
| Page | URL pattern | Layout type | Example |
|---|---|---|---|
| **Term page** (one per term) | `/{taxonomy}/{slugified-term}/` | `term` | `/tags/lunet/` |
| **Terms listing** (one per taxonomy) | `/{taxonomy}/` | `terms` | `/tags/` |

If a taxonomy has no pages assigned to any term, no pages are generated.

## Layouts

You must provide layout files for each taxonomy. The layouts plugin resolves them by name:

{.table}
| Layout file | Purpose |
|---|---|
| `tags.term.html` | Renders an individual tag page |
| `tags.terms.html` | Renders the list of all tags |
| `categories.term.html` | Renders an individual category page |
| `categories.terms.html` | Renders the list of all categories |

The naming convention is `{taxonomy_name}.{layout_type}.{ext}`.

### Variables available in layouts

**In `term` layouts** (individual term page):

{.table}
| Variable | Type | Description |
|---|---|---|
| `term` | TaxonomyTerm | The current term |
| `term.name` | string | Original term name from front matter |
| `term.url` | string | URL of this term page |
| `term.pages` | PageCollection | Pages tagged with this term (sorted) |
| `taxonomy` | Taxonomy | The parent taxonomy |
| `pages` | PageCollection | Same as `term.pages` |

**In `terms` layouts** (taxonomy listing page):

{.table}
| Variable | Type | Description |
|---|---|---|
| `taxonomy` | Taxonomy | The current taxonomy |
| `taxonomy.name` | string | Taxonomy name (e.g. `"tags"`) |
| `taxonomy.url` | string | Base URL of the taxonomy |

## Accessing taxonomies in templates

After processing, taxonomies are available as `site.taxonomies`:

```scriban
# Iterate all tags sorted alphabetically
for term in site.taxonomies.tags.terms.by_name
  # term.name, term.url, term.pages are available
end

# Iterate tags sorted by popularity (most pages first)
for term in site.taxonomies.tags.terms.by_count
  # term.name, term.pages.size are available
end

# Access a specific term
tag = site.taxonomies.tags.terms["lunet"]
```

### Taxonomy object properties

{.table}
| Property | Type | Description |
|---|---|---|
| `name` | string | Plural name (e.g. `"tags"`) |
| `url` | string | Base URL path |
| `single` | string | Singular form |
| `terms` | object | Dictionary of term name → TaxonomyTerm |
| `terms.by_name` | collection | Terms sorted alphabetically (case-sensitive) |
| `terms.by_count` | collection | Terms sorted by page count (descending) |

### TaxonomyTerm object properties

{.table}
| Property | Type | Description |
|---|---|---|
| `name` | string | Original term name from front matter |
| `url` | string | Full URL path for this term's page |
| `pages` | PageCollection | Pages associated with this term |

## Error handling

- If a page's front-matter value for a taxonomy key is not an array, the page is skipped for that taxonomy with an error logged.
- If a singular name is null or whitespace, the taxonomy is skipped.
- Empty taxonomies (no terms with pages) do not generate any output pages.

## See also

- [Layouts and includes](/docs/layouts-and-includes/) — layout files required for taxonomy pages
- [Content and front matter](/docs/content-and-frontmatter/) — how to set taxonomy values in pages
- [Menus module](menus.md) — for navigation including taxonomy links

