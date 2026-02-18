---
title: "Cards module (OpenGraph/Twitter)"
---

# Cards module (OpenGraph/Twitter)

The cards module injects social media and SEO meta tags into your page `<head>`. It supports **Twitter Cards** and **OpenGraph** (OG) meta tags, which control how links to your site appear when shared on social media platforms.

The cards template is automatically included in the `<head>` of every page — no manual include is needed. Output is gated by the `enable` flags below.

## Enable

> All `cards.*` properties shown on this page are set inside your site's `config.scriban`, within a `with cards` / `end` block.

```scriban
with cards
  with twitter
    enable = true
    card = "summary_large_image"
    user = "yourhandle"
    image = "/img/social.png"
    image_alt = "Site logo"
  end
  with og
    enable = true
    type = "website"
    image = "/img/social.png"
    image_alt = "Site logo"
    locale = "en_US"
  end
end
```

## Twitter Cards configuration

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `cards.twitter.enable` | bool | `false` | Enable Twitter Card meta tags |
| `cards.twitter.card` | string | `"summary"` | Card type: `"summary"`, `"summary_large_image"`, `"app"`, or `"player"` |
| `cards.twitter.user` | string | *(none)* | Twitter handle **without** the `@` prefix (it is added automatically) |
| `cards.twitter.title` | string | *(page title)* | Default title for `twitter:title` |
| `cards.twitter.description` | string | *(page summary)* | Default description for `twitter:description` |
| `cards.twitter.image` | string | *(none)* | URL to the default card image |
| `cards.twitter.image_alt` | string | *(title)* | Alt text for the card image |

> **Note:** Do not include `@` in the `user` value — it is prepended automatically. Setting `user = "@handle"` would produce `@@handle`.

## OpenGraph configuration

{.table}
| Property | Type | Default | Description |
|---|---|---|---|
| `cards.og.enable` | bool | `false` | Enable OpenGraph meta tags |
| `cards.og.type` | string | `"article"` | OG content type (e.g. `"website"`, `"article"`) |
| `cards.og.title` | string | *(page title)* | Default title for `og:title` |
| `cards.og.description` | string | *(page summary)* | Default description for `og:description` |
| `cards.og.image` | string | *(none)* | URL to the default OG image |
| `cards.og.image_alt` | string | *(title)* | Alt text for the OG image |
| `cards.og.locale` | string | *(none)* | Locale tag (e.g. `"en_US"`) |

The `og:url` tag is always generated from the current page URL when OG is enabled.

## Per-page overrides

Override any card property in page front matter. Page values take priority over site-level configuration:

{.table}
| Front matter key | Overrides |
|---|---|
| `twitter_card` | `cards.twitter.card` |
| `twitter_user` | `cards.twitter.user` |
| `twitter_title` | `cards.twitter.title` |
| `twitter_description` | `cards.twitter.description` |
| `twitter_image` | `cards.twitter.image` |
| `twitter_image_alt` | `cards.twitter.image_alt` |
| `og_type` | `cards.og.type` |
| `og_title` | `cards.og.title` |
| `og_description` | `cards.og.description` |
| `og_image` | `cards.og.image` |
| `og_image_alt` | `cards.og.image_alt` |

Example:

```yaml
og_type: website
twitter_title: "Custom title for sharing"
twitter_image: "/img/custom-card.png"
```

## Title and description fallback

The cards template uses a fallback chain for titles and descriptions:

**Title** (first non-empty value wins):

1. Page-level override (e.g. `twitter_title` or `og_title`)
2. Site-level card title (e.g. `cards.twitter.title`)
3. `page.full_title`
4. `site.html.head.title`
5. `page.title + " - " + site.title`

**Description** (first non-empty value wins):

1. Page-level override (e.g. `twitter_description`)
2. `page.summary` (from the [Summarizer module](summarizer.md), evaluated lazily with HTML quote escaping)
3. Site-level card description
4. `site.description`

## Image tags

Image meta tags (`twitter:image`, `og:image`) are only rendered when an image URL is configured at either the site or page level. Image URLs are resolved through Lunet's URL reference system.

## See also

- [Summarizer module](summarizer.md) — provides `page.summary` used for card descriptions
- [Tracking module](tracking.md) — other `<head>` meta tag injection

