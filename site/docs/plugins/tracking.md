---
title: "Tracking module (Google Analytics)"
---

# Tracking module (Google Analytics)

The tracking module injects Google Analytics (GA4 / gtag.js) into your site's `<head>`. It is automatically included — no manual template setup is needed.

## Configure

Set your Google Analytics measurement ID in `config.scriban`:

```scriban
site.tracking.google.id = "G-XXXXXXXXXX"
```

Both GA4 (`G-...`) and legacy Universal Analytics (`UA-...`) IDs are accepted.

## What gets injected

When active, the module emits two `<script>` tags in the page `<head>`:

1. The async gtag.js loader from `googletagmanager.com`
2. An inline snippet that initializes the data layer and calls `gtag('config', '<your-id>')`

## Environment behavior

The tracking snippet is emitted **only** when both conditions are met:

- `site.environment == "prod"` (case-sensitive — `"Prod"` or `"PROD"` will not match)
- `site.tracking.google.id` is set to a truthy value

This makes `lunet serve` safe by default — it runs in `dev` mode, so no analytics code is injected during development.

When building for production, use `lunet build` (which defaults to `prod` environment) or set the environment explicitly.

## Customizing the snippet

The tracking snippet is rendered from the include file `_builtins/google-analytics.sbn-html`. You can override this file by placing your own version at `.lunet/includes/_builtins/google-analytics.sbn-html` in your site. This lets you customize the tracking code (e.g. adding custom dimensions or event parameters).

## See also

- [Cards module](cards.md) — other `<head>` meta tag injection
- [Configuration](/docs/configuration/) — setting `site.environment` and other site properties
