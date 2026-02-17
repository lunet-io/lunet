(() => {
  const parseList = (value) => {
    if (!value) return [];
    return value
      .split(",")
      .map((x) => x.trim())
      .filter((x) => x.length > 0);
  };

  const escapeId = (id) => {
    if (typeof CSS !== "undefined" && typeof CSS.escape === "function") {
      return CSS.escape(id);
    }
    return id.replace(/[^a-zA-Z0-9_-]/g, "\\$&");
  };

  const applyOpenState = (root, openIds) => {
    for (const id of openIds) {
      const list = root.querySelector(`#${escapeId(id)}`);
      if (!list) continue;
      list.classList.add("show");

      const toggle = root.querySelector(`[href="#${escapeId(id)}"][data-bs-toggle="collapse"]`);
      if (toggle) {
        toggle.setAttribute("aria-expanded", "true");
        toggle.classList.remove("collapsed");
      }
    }
  };

  const applyActiveState = (root, activeItemIds) => {
    for (const id of activeItemIds) {
      const item = root.querySelector(`#${escapeId(id)}`);
      if (item) {
        item.classList.add("active");
      }
    }
  };

  const loadMenu = async (host) => {
    const partialUrl = host.dataset.lunetMenuPartial;
    if (!partialUrl) return;

    const openIds = parseList(host.dataset.lunetMenuOpen);
    const activeIds = parseList(host.dataset.lunetMenuActive);

    try {
      const res = await fetch(partialUrl, { cache: "force-cache" });
      if (!res.ok) return;

      host.innerHTML = await res.text();
      applyOpenState(host, openIds);
      applyActiveState(host, activeIds);
    } catch {
      // Keep fallback HTML (no-JS / offline / transient errors).
    }
  };

  const init = () => {
    const menus = document.querySelectorAll(".lunet-menu-async[data-lunet-menu-partial]");
    for (const host of menus) {
      void loadMenu(host);
    }
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
