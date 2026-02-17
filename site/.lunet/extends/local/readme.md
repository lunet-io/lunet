---
title: Home
layout: simple
og_type: website
---

# New Website Quickstart

This is the default landing page for a new website using this template.

Use it as a setup checklist, then replace it with your real homepage content.

## 1) Create `config.scriban`

Reference the template and set project values:

```scriban
extend "lunet-io/lunet_template@1.0.0"

site_project_name = "MyProject"
site_project_description = "Short project description."
site_project_logo_path = "/img/myproject-logo.png"
site_project_social_banner_path = "/img/myproject-banner.png"
site_project_banner_background_path = "/img/myproject-banner-background.png"
site_project_package_id = "MyProject"
site_project_github_user = "org"
site_project_github_repo = "myproject"
site_project_basepath = "/myproject"

# Optional: override the default favicon path (/favicon.ico)
site_project_favicon_path = "/favicon.ico"

site_project_init
```

Notes:

- `site_project_logo_path` defaults to `/img/lunet-logo.png` if not set.
- Favicon defaults to `/favicon.ico`. Keep a file there, or override `site_project_favicon_path`.

### Theme defaults

```scriban
template_theme_default_mode = "system" # system, light, dark
template_theme_storage_key = "lunet-theme"
```

Use the navbar theme button to switch mode at runtime.

## Theme configuration and customization (consumer guide)

When this theme is used from a remote repository, you should treat theme internals as read-only.

Customize through your own site files and `config.scriban` only.

### Naming conventions (to avoid confusion)

- `site_project_*`: **project inputs** you set in your site `config.scriban` (name, repo, owner, assets…).
- `template_*`: **theme customization** knobs (labels, theme mode, extra override styles…).
- `project_*`: **resolved values** computed by `site_project_init` and used by the theme layouts (don’t set these manually).
- Lunet core variables like `baseurl`, `basepath`, `title`, `description`, `author` are set by `site_project_init` (but you can also override them directly if needed).

### Theme variables (`config.scriban`)

Set these variables before calling `site_project_init`:

{.table}
| Variable | Type | Default | Meaning |
|---|---:|---:|---|
| `template_theme_default_mode` | string | `"system"` | Initial theme mode (`"system"`, `"light"`, `"dark"`). |
| `template_theme_storage_key` | string | `"lunet-theme"` | LocalStorage key used by the theme switcher. |
| `template_theme_override_styles` | list of strings | `[]` | Site-owned stylesheet paths bundled **after** the theme CSS (recommended way to override colors). |

### Project variables (`config.scriban`)

These variables are project/site metadata used by the theme:

{.table}
| Variable | Example | Used for |
|---|---:|---|
| `site_project_name` | `"MyProject"` | Site title/branding. |
| `site_project_description` | `"Short description"` | Homepage and social metadata. |
| `site_project_logo_path` | `"/img/myproject-logo.png"` | Navbar logo. |
| `site_project_social_banner_path` | `"/img/myproject-banner.png"` | Social/OG image. |
| `site_project_banner_background_path` | `"/img/myproject-banner-background.png"` | Homepage banner background. |
| `site_project_package_id` | `"MyProject"` | Package display/links (when enabled). |
| `site_project_baseurl` | `"https://example.com"` | Canonical URL used for `lunet build` (do not set core `baseurl`; `lunet serve` uses localhost). |
| `site_project_github_user` | `"org"` | GitHub org/user (used to build repo URLs). |
| `site_project_github_repo` | `"myproject"` | GitHub repo name (used to build repo URLs). |
| `site_project_basepath` | `"/myproject"` | Base path when hosted under a sub-path (e.g. GitHub Pages project site). |
| `site_project_favicon_path` | `"/favicon.ico"` | Favicon path used by the theme `<head>` includes. |
| `site_project_owner_name` | `"Your Name"` | Footer ownership + author metadata. |
| `site_project_owner_alias` | `"your-handle"` | Footer ownership alias (linked). |
| `site_project_owner_url` | `"https://example.com"` | Footer ownership link target. |
| `site_project_content_license_name` | `"CC BY 2.5"` | Footer content license label. |
| `site_project_content_license_url` | `"http://creativecommons.org/licenses/by/2.5/"` | Footer content license link target. |
| `site_project_twitter_user` | `"your-handle"` | Twitter card metadata. |

### 1) Configure the theme in `config.scriban`

```scriban
extend "lunet-io/lunet_template@1.0.0"

template_theme_default_mode = "system"   # system, light, dark
template_theme_storage_key = "lunet-theme"
template_theme_override_styles = ["/css/theme-overrides.scss"]

site_project_name = "MyProject"
site_project_description = "Short project description."
site_project_logo_path = "/img/myproject-logo.png"
site_project_social_banner_path = "/img/myproject-banner.png"
site_project_banner_background_path = "/img/myproject-banner-background.png"
site_project_package_id = "MyProject"
site_project_github_user = "org"
site_project_github_repo = "myproject"
site_project_basepath = "/myproject"

site_project_init
```

`template_theme_override_styles` is the supported extension point to load your site-owned styles after the theme styles.

Notes:

- Paths in `template_theme_override_styles` are site URLs (e.g. `"/css/theme-overrides.scss"`), not filesystem paths.
- Use `.scss` if you want Sass features (variables, nesting); plain `.css` also works.

### 2) Create your site-level override stylesheet

Create `site/css/theme-overrides.scss` in your own project:

```scss
:root[data-bs-theme="dark"] {
  /* Core surfaces */
  --lunet-color-background: #0b1020;
  --lunet-color-foreground: #e7ecff;
  --lunet-color-surface-1: #0f1830;
  --lunet-color-border: rgba(255, 255, 255, 0.10);

  /* Accents */
  --lunet-color-blue: #6ea8ff;
  --lunet-color-magenta: #ff5bd6;

  /* Code (Prism tokens) */
  --lunet-code-bg: #0b1426;
  --lunet-prism-text: #e9eeff;
  --lunet-prism-keyword: #7aa2ff;
  --lunet-prism-string: #a6e3a1;
  --lunet-prism-comment: #7c8aa5;
}

:root[data-bs-theme="light"] {
  /* Core surfaces */
  --lunet-color-background: #f7f9ff;
  --lunet-color-foreground: #121828;
  --lunet-color-surface-1: #ffffff;
  --lunet-color-border: rgba(0, 0, 0, 0.10);

  /* Accents */
  --lunet-color-blue: #2459d2;
  --lunet-color-magenta: #b62cff;

  /* Code (Prism tokens) */
  --lunet-code-bg: #f2f5ff;
  --lunet-prism-text: #10162a;
  --lunet-prism-keyword: #305fdf;
  --lunet-prism-string: #1b7f3a;
  --lunet-prism-comment: #6a738b;
}
```

This approach keeps your branding in your site repo while allowing safe theme upgrades.

### 3) What can be overridden

The theme exposes CSS custom properties (`--lunet-*`) so you can override without editing theme files.

#### Core UI variables

Commonly overridden variables:

{.table}
| Variable | Meaning |
|---|---|
| `--lunet-color-background` | Page background. |
| `--lunet-color-foreground` | Default text color. |
| `--lunet-color-surface-1` | Surface background for cards/blocks. |
| `--lunet-color-border` | Default border color. |
| `--lunet-color-blue` | Primary accent (links, highlights). |
| `--lunet-color-cyan` | Secondary accent. |
| `--lunet-color-magenta` | Accent used in gradients/branding. |
| `--lunet-color-green` / `--lunet-color-yellow` / `--lunet-color-red` | Status/semantic colors. |

#### Prism (syntax highlighting) variables

The Prism theme in this template is driven by variables so it can follow light/dark mode.

{.table}
| Variable | Used for |
|---|---|
| `--lunet-code-bg` | Code block background. |
| `--lunet-code-selection-bg` | Text selection background inside code blocks. |
| `--lunet-code-inline-bg` | Inline code background. |
| `--lunet-prism-text` | Default code text. |
| `--lunet-prism-inline-color` | Inline code text color. |
| `--lunet-prism-comment` | Comments/doc comments. |
| `--lunet-prism-string` | Strings/chars. |
| `--lunet-prism-number` | Numbers. |
| `--lunet-prism-keyword` | Keywords. |
| `--lunet-prism-function` | Function names. |
| `--lunet-prism-keyword-alt` | Secondary keyword color. |
| `--lunet-prism-class-name` | Class/type identifiers. |

### 4) Concrete override patterns

#### Make the whole site slightly brighter (dark mode)

```scss
:root[data-bs-theme="dark"] {
  --lunet-color-background: #0c1822;
  --lunet-color-surface-1: #122235;
}
```

#### Change only link + gradient accents

```scss
:root {
  --lunet-color-blue: #4aa3ff;
  --lunet-color-magenta: #ff4bd8;
}
```

#### Make code blocks more readable

```scss
:root[data-bs-theme="dark"] {
  --lunet-code-bg: #0b1426;
  --lunet-prism-comment: #8b97b2;
  --lunet-prism-text: #e9eeff;
}
```

### 5) Guidance (upgrade-safe customization)

- Prefer overriding `--lunet-*` variables over targeting deep selectors (theme internals change more often).
- Keep overrides scoped to your own file(s) referenced by `template_theme_override_styles`.
- If you need layout changes, do them in your own CSS (but keep them minimal so theme updates don’t break your site).

## 2) Create top navigation `menu.yml`

Define the left and right navbar menus:

```yml
home:
  - {path: readme.md, title: "<i class='bi bi-house-door' aria-hidden='true'></i> Home"}
  - {path: docs/readme.md, title: "<i class='bi bi-book' aria-hidden='true'></i> Docs", folder: true}

home2:
  - {url: "https://github.com/<org>/<repo>/", title: '<i class="bi bi-github"></i> GitHub', link_class: btn btn-info}
```

- `home` is rendered on the left.
- `home2` is rendered on the right.

## 3) Add content pages

Typical starting structure:

```text
site/
  config.scriban
  menu.yml
  readme.md
  img/
    myproject-logo.png
    favicon.ico
  docs/
    readme.md
    getting-started.md
    menu.yml
```

## 4) Add docs menu (`docs/menu.yml`)

For docs pages, define a local docs menu:

```yml
doc:
  - {path: readme.md, title: "<i class='bi bi-book' aria-hidden='true'></i> User Guide"}
  - {path: getting-started.md, title: "<i class='bi bi-rocket-takeoff' aria-hidden='true'></i> Getting Started"}
```

## 5) Replace this default homepage

`readme.md` is the homepage (`/`). Keep frontmatter and replace this body with project-specific content.

## Build locally

```bash
lunet build
```

or:

```bash
lunet serve
```

