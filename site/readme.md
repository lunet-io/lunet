---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5">
  <div class="container">
    <img src="{{site.basepath}}/img/lunet-hero-banner.svg" alt="Lunet — Fast, modular static site generator" class="img-fluid" style="width: min(100%, 48rem); height: auto;">
    <p class="lead mt-4 mb-4">
      A fast, modular static site generator.<br>
      Configuration is <strong>executable Scriban code</strong> — not YAML-only config.
    </p>
    <div class="d-flex justify-content-center gap-3 mt-4 flex-wrap">
      <a href="{{site.basepath}}/docs/getting-started/" class="btn btn-primary btn-lg"><i class="bi bi-rocket-takeoff"></i> Get started</a>
      <a href="{{site.basepath}}/docs/" class="btn btn-outline-secondary btn-lg"><i class="bi bi-book"></i> Documentation</a>
      <a href="https://github.com/lunet-io/lunet" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="mt-4 text-start mx-auto" style="max-width: 48rem;">
      <pre class="language-shell-session"><code>dotnet tool install -g lunet</code></pre>
      <p class="text-center text-secondary mt-2" style="font-size: 0.85rem;">Requires the <a href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0" class="text-secondary">.NET 10 SDK</a></p>
    </div>
  </div>
</section>

<section class="container my-5">
  <div class="row row-cols-1 row-cols-lg-2 gx-5 gy-4">
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-code-slash lunet-feature-icon lunet-icon--controls"></i> Scriban templating</div>
        <div class="card-body">
          <p class="card-text">
            Pages, layouts, and even <code>config.scriban</code> are full Scriban scripts — loops, conditionals, functions, and custom variables everywhere.
          </p>

[Configuration](docs/configuration.md) · [Content &amp; front matter](docs/content-and-frontmatter.md)

</div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-palette lunet-feature-icon lunet-icon--themes"></i> Themes &amp; extensions</div>
        <div class="card-body">
          <p class="card-text">
            Install themes from any GitHub repo with a single line. A layered virtual filesystem lets you override any theme file at the same path.
          </p>

[Themes &amp; extends](docs/themes-and-extends.md) · [Extends module](docs/plugins/extends.md)

</div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-filetype-scss lunet-feature-icon lunet-icon--editing"></i> SCSS, bundles &amp; npm</div>
        <div class="card-body">
          <p class="card-text">
            Compile SCSS via the embedded Dart Sass compiler, bundle &amp; minify CSS/JS, and fetch npm packages (Bootstrap, Font Awesome…) without a <code>node_modules</code> workflow.
          </p>

[SCSS](docs/plugins/scss.md) · [Bundles](docs/plugins/bundles.md) · [Resources](docs/plugins/resources.md)

</div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-rss lunet-feature-icon lunet-icon--data"></i> RSS, sitemaps &amp; search</div>
        <div class="card-body">
          <p class="card-text">
            Generate RSS feeds, <code>sitemap.xml</code>, <code>robots.txt</code>, and a client-side search index — all out of the box.
          </p>

[RSS](docs/plugins/rss.md) · [Sitemaps](docs/plugins/sitemaps.md) · [Search](docs/plugins/search.md)

</div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-eyeglasses lunet-feature-icon lunet-icon--chrome"></i> Live reload</div>
        <div class="card-body">
          <p class="card-text">
            Built-in dev server with file watcher. Edit a page and see changes in your browser instantly.
          </p>

[Server](docs/plugins/server.md) · [Watcher](docs/plugins/watcher.md)

</div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-filetype-cs lunet-feature-icon lunet-icon--binding"></i> .NET API docs</div>
        <div class="card-body">
          <p class="card-text">
            Generate API reference pages directly from .NET projects or assemblies — a feature unique to Lunet.
          </p>

[API (.NET)](docs/plugins/api-dotnet.md) · [Logging API](https://xenoatom.github.io/logging/api/) · [CommandLine API](https://xenoatom.github.io/commandline/api/) · [Terminal API](https://xenoatom.github.io/terminal/api/)

</div>
      </div>
    </div>
  </div>
</section>

<section class="container my-5">
  <div class="card">
    <div class="card-header display-6">
      <i class="bi bi-box-seam lunet-feature-icon lunet-icon--lists"></i> And more…
    </div>
    <div class="card-body">
      <p class="card-text">
        Taxonomies, menus, SEO/social cards, Google Analytics, Markdown via <a href="https://github.com/xoofx/markdig">Markdig</a>, data loading (YAML / JSON / TOML), URL pattern rules, automatic page summaries — all as small, composable modules.
      </p>

[Browse all modules](docs/plugins/readme.md)

</div>
  </div>
</section>

<section class="container my-5">
  <div class="card">
    <div class="card-header display-6">
      <i class="bi bi-github lunet-feature-icon lunet-icon--chrome"></i> One-click deploy with GitHub Actions
    </div>
    <div class="card-body">
      <p class="card-text">
        Add a single workflow file to your repository and Lunet builds &amp; deploys your site to <strong>GitHub Pages</strong> automatically — no build scripts needed.
      </p>

[Publishing with GitHub Actions](docs/github-actions.md)

</div>
  </div>
</section>
