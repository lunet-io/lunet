---
title: "Tracking module (Google Analytics)"
---

# Tracking module (Google Analytics)

The tracking module can inject Google Analytics.

## Configure

```scriban
site.tracking.google.id = "G-XXXXXXXXXX"
```

## Environment behavior

Analytics is emitted only when:
- `site.environment == "prod"`
- and `site.tracking.google.id` is set

This makes `lunet serve` safe by default (it runs in `dev`).

