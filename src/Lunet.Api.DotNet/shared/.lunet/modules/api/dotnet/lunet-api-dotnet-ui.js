(() => {
  "use strict";

  function normalizeKind(text) {
    var kind = (text || "").trim().toLowerCase();
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
    var tokens = (query || "")
      .trim()
      .split(/\s+/)
      .filter(Boolean);

    var kinds = [];
    var textTokens = [];

    for (var index = 0; index < tokens.length; index++) {
      var token = tokens[index];
      var idx = token.indexOf(":");
      if (idx > 0) {
        var key = token.slice(0, idx).toLowerCase();
        var value = token.slice(idx + 1);
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
    var input = filterRoot.querySelector(".api-dotnet-member-filter-input");
    if (!input) return;

    var scope = filterRoot.closest(".api-dotnet") || document;
    var items = scope.querySelectorAll(".api-dotnet-member-item");
    var itemCount = items.length;
    if (itemCount === 0) return;

    for (var index = 0; index < itemCount; index++) {
      var item = items[index];
      if (!item.dataset.apiSearchText) {
        item.dataset.apiSearchText = (item.textContent || "").toLowerCase();
      }
      if (!item.dataset.apiKind && item.getAttribute("data-api-kind")) {
        item.dataset.apiKind = normalizeKind(item.getAttribute("data-api-kind"));
      } else if (item.dataset.apiKind) {
        item.dataset.apiKind = normalizeKind(item.dataset.apiKind);
      }
    }

    var status = filterRoot.querySelector(".api-dotnet-member-filter-status");
    if (!status) {
      status = document.createElement("div");
      status.className = "api-dotnet-member-filter-status";
      status.setAttribute("aria-live", "polite");
      filterRoot.appendChild(status);
    }

    function apply() {
      var q = parseQuery(input.value);
      var visible = 0;

      for (var index = 0; index < itemCount; index++) {
        var item = items[index];
        var kindOk =
          q.kinds.length === 0 ||
          q.kinds.includes(item.dataset.apiKind || "");
        var textOk =
          q.text === "" || (item.dataset.apiSearchText || "").includes(q.text);
        var show = kindOk && textOk;
        item.hidden = !show;
        if (show) visible++;
      }

      status.textContent =
        q.kinds.length === 0 && q.text === ""
          ? ""
          : visible + " / " + itemCount + " shown";
    }

    input.addEventListener("input", apply, { passive: true });
    input.addEventListener("keydown", function (ev) {
      if (ev.key === "Escape") {
        input.value = "";
        apply();
      }
    });

    apply();
  }

  function init() {
    var filters = document.querySelectorAll("[data-api-dotnet-filter]");
    for (var index = 0; index < filters.length; index++) {
      setupFilter(filters[index]);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init, { once: true });
  } else {
    init();
  }
})();
