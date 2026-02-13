---
title: "Markdown module"
---

# Markdown module

The Markdown module converts Markdown pages to HTML and integrates with Lunet’s layout system.

## Markdown pages

Add front matter to a `.md` file to make it a page:

```markdown
---
title: "Hello"
---

# Hello
This is **Markdown**.
```

## Options

```scriban
with markdown
  options.extensions = "advanced"
  options.css_img_attr = "img-fluid"
end
```

## `markdown.to_html(...)`

The module exposes a helper to convert arbitrary markdown strings:

```scriban
{{ '{{ markdown.to_html "# Hello" }}' }}
```
