(() => {
  "use strict";

  function normalizeKind(text) {
    const kind = (text || "").trim().toLowerCase();
    switch (kind) {
      case "ctor":
      case "constructor":
      case "constructors":
        return "constructor";
      case "field":
      case "fields":
        return "field";
      case "prop":
      case "property":
      case "properties":
        return "property";
      case "method":
      case "methods":
        return "method";
      case "event":
      case "events":
        return "event";
      case "operator":
      case "operators":
        return "operator";
      case "extension":
      case "extensions":
        return "extension";
      case "eii":
      case "eiimethod":
      case "explicit":
        return "eiimethod";
      default:
        return kind;
    }
  }

  function parseQuery(query) {
    const tokens = (query || "")
      .trim()
      .split(/\s+/)
      .filter(Boolean);

    const kinds = [];
    const textTokens = [];

    for (let index = 0; index < tokens.length; index++) {
      const token = tokens[index];
      const idx = token.indexOf(":");
      if (idx > 0) {
        const key = token.slice(0, idx).toLowerCase();
        const value = token.slice(idx + 1);
        if (key === "kind" && value) {
          kinds.push(normalizeKind(value));
          continue;
        }
      }
      textTokens.push(token);
    }

    return {
      kinds,
      text: textTokens.join(" ").toLowerCase(),
    };
  }

  function setupFilter(filterRoot) {
    const input = filterRoot.querySelector(".api-dotnet-member-filter-input");
    if (!input) return;

    const scope = filterRoot.closest(".api-dotnet") || document;
    const items = scope.querySelectorAll(".api-dotnet-member-item");
    const itemCount = items.length;
    if (itemCount === 0) return;

    for (let index = 0; index < itemCount; index++) {
      const item = items[index];
      if (!item.dataset.apiSearchText) {
        item.dataset.apiSearchText = (item.textContent || "").toLowerCase();
      }
      if (!item.dataset.apiKind && item.getAttribute("data-api-kind")) {
        item.dataset.apiKind = normalizeKind(item.getAttribute("data-api-kind"));
      } else if (item.dataset.apiKind) {
        item.dataset.apiKind = normalizeKind(item.dataset.apiKind);
      }
    }

    const status = document.createElement("div");
    status.className = "api-dotnet-member-filter-status";
    status.setAttribute("aria-live", "polite");
    filterRoot.appendChild(status);

    function apply() {
      const q = parseQuery(input.value);
      let visible = 0;

      for (let index = 0; index < itemCount; index++) {
        const item = items[index];
        const kindOk =
          q.kinds.length === 0 ||
          q.kinds.includes(item.dataset.apiKind || "");
        const textOk =
          q.text === "" || (item.dataset.apiSearchText || "").includes(q.text);
        const show = kindOk && textOk;
        item.hidden = !show;
        if (show) visible++;
      }

      status.textContent =
        q.kinds.length === 0 && q.text === ""
          ? ""
          : `${visible} / ${itemCount} shown`;
    }

    input.addEventListener("input", apply, { passive: true });
    input.addEventListener("keydown", (ev) => {
      if (ev.key === "Escape") {
        input.value = "";
        apply();
      }
    });

    apply();
  }

  function init() {
    const filters = document.querySelectorAll("[data-api-dotnet-filter]");
    for (let index = 0; index < filters.length; index++) {
      setupFilter(filters[index]);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init, { once: true });
  } else {
    init();
  }
})();
