---
title: Home
layout: simple
og_type: website
---

# New Website Quickstart

This is the default landing page created by `lunet init`.

Use it as a setup checklist, then replace it with your real homepage content.

## 1) Edit `config.scriban`

Your site already references the [default Lunet template](https://github.com/lunet-io/templates). Open `config.scriban` and set your project values:

```scriban
extend "lunet-io/templates"

site_project_name = "MyProject"
site_project_description = "Short project description."
site_project_baseurl = "https://example.com"
site_project_github_user = "org"
site_project_github_repo = "myproject"
site_project_owner_name = "Your Name"

# Optional — set a logo, social banner, favicon, etc.:
# site_project_logo_path = "/img/myproject-logo.png"
# site_project_social_banner_path = "/img/myproject-banner.png"
# site_project_banner_background_path = "/img/myproject-banner-background.png"
# site_project_favicon_path = "/favicon.ico"
# site_project_basepath = "/myproject"  # only if hosted under a sub-path

site_project_init
```

Notes:

- `site_project_logo_path` defaults to the Lunet logo if not set.
- Favicon defaults to `/favicon.ico`. Keep a file there, or override `site_project_favicon_path`.

### Theme defaults

```scriban
template_theme_default_mode = "system" # system, light, dark
template_theme_storage_key = "lunet-theme"
```

Use the navbar theme button to switch mode at runtime.

## Theme configuration and customization

The template provides layouts, CSS/JS assets, a theme switcher, search, and more. Treat theme internals as read-only — customize through your own files and `config.scriban` only.

### Naming conventions

- `site_project_*`: **project inputs** you set in your `config.scriban` (name, repo, owner, assets…).
- `template_*`: **theme customization** knobs (labels, theme mode, extra override styles…).
- `project_*`: **resolved values** computed by `site_project_init` (don't set these manually).
- Lunet core variables like `baseurl`, `basepath`, `title`, `description`, `author` are set by `site_project_init`.

### Theme variables

Set these before calling `site_project_init`:

{.table}
| Variable | Type | Default | Meaning |
|---|---:|---:|---|
| `template_theme_default_mode` | string | `"system"` | Initial theme mode (`"system"`, `"light"`, `"dark"`). |
| `template_theme_storage_key` | string | `"lunet-theme"` | LocalStorage key used by the theme switcher. |
| `template_theme_override_styles` | list of strings | `[]` | Site-owned stylesheet paths bundled **after** the theme CSS. |

### Project variables

{.table}
| Variable | Example | Used for |
|---|---:|---|
| `site_project_name` | `"MyProject"` | Site title/branding. |
| `site_project_description` | `"Short description"` | Homepage and social metadata. |
| `site_project_logo_path` | `"/img/myproject-logo.png"` | Navbar logo. |
| `site_project_social_banner_path` | `"/img/myproject-banner.png"` | Social/OG image. |
| `site_project_banner_background_path` | `"/img/myproject-banner.png"` | Homepage banner background. |
| `site_project_package_id` | `"MyProject"` | Package display/links. |
| `site_project_baseurl` | `"https://example.com"` | Canonical URL for `lunet build`. |
| `site_project_github_user` | `"org"` | GitHub org/user. |
| `site_project_github_repo` | `"myproject"` | GitHub repo name. |
| `site_project_basepath` | `"/myproject"` | Base path (sub-path hosting). |
| `site_project_favicon_path` | `"/favicon.ico"` | Favicon path. |
| `site_project_owner_name` | `"Your Name"` | Footer ownership + author. |
| `site_project_owner_alias` | `"your-handle"` | Footer ownership alias. |
| `site_project_owner_url` | `"https://example.com"` | Footer ownership link. |
| `site_project_content_license_name` | `"CC BY 2.5"` | Footer content license. |
| `site_project_content_license_url` | `"http://creativecommons.org/licenses/by/2.5/"` | License link. |
| `site_project_twitter_user` | `"your-handle"` | Twitter card metadata. |

### Custom styles

Add `template_theme_override_styles` to load your own CSS/SCSS after the theme:

```scriban
template_theme_override_styles = ["/css/theme-overrides.scss"]
```

Then create `css/theme-overrides.scss` in your site folder:

```scss
:root[data-bs-theme="dark"] {
  --lunet-color-background: #0b1020;
  --lunet-color-blue: #6ea8ff;
  --lunet-code-bg: #0b1426;
}
```

The theme exposes `--lunet-*` CSS custom properties for colors, code highlighting, and more. Override these instead of targeting internal selectors.

## 2) Edit navigation

### Top bar (`menu.yml`)

Your site includes a `menu.yml` with Home and Docs entries. Add right-side links:

```yml
home:
  - {path: readme.md, title: "<i class='bi bi-house-door' aria-hidden='true'></i> Home", self: true}
  - {path: docs/readme.md, title: "<i class='bi bi-book' aria-hidden='true'></i> Docs", folder: true}

home2:
  - {url: "https://github.com/org/repo/", title: '<i class="bi bi-github"></i> GitHub', link_class: btn btn-info}
```

### Docs sidebar (`docs/menu.yml`)

Add pages to the docs section:

```yml
doc:
  - {path: readme.md, title: "<i class='bi bi-book' aria-hidden='true'></i> Documentation"}
  - {path: getting-started.md, title: "<i class='bi bi-rocket-takeoff' aria-hidden='true'></i> Getting Started"}
```

## 3) Replace this homepage

This `readme.md` is the homepage (`/`). Keep the front matter and replace the body with your project content.

## Build locally

```shell-session
lunet serve
```

Your site is live at `http://localhost:4000`. Edit pages and see changes reload instantly.

Build for production:

```shell-session
lunet build
```

Output goes to `.lunet/build/www/`.
