const lunetCssPrefix = document.documentElement.dataset.lunetCssPrefix || 'lunet';

// Ensure copy-to-clipboard buttons get Bootstrap tooltips
document.addEventListener('DOMContentLoaded', () => {
  const rootElement = document.documentElement;
  const themeStorageKey = rootElement.dataset.lunetThemeStorageKey || 'lunet-theme';
  const defaultThemeMode = rootElement.dataset.lunetThemeMode || 'system';
  const themeMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  const themeModeButtons = document.querySelectorAll('[data-lunet-theme-choice]');
  const themeToggleIcon = document.querySelector('[data-lunet-theme-toggle-icon]');
  const themeToggleLabel = document.querySelector('[data-lunet-theme-toggle-label]');
  const themeModeLabels = {
    system: themeToggleLabel?.dataset.themeSystemLabel || 'System',
    light: themeToggleLabel?.dataset.themeLightLabel || 'Light',
    dark: themeToggleLabel?.dataset.themeDarkLabel || 'Dark',
  };

  function sanitizeThemeMode(mode) {
    return mode === 'light' || mode === 'dark' || mode === 'system' ? mode : defaultThemeMode;
  }

  function resolveThemeMode(mode) {
    if (mode === 'system') {
      return themeMediaQuery.matches ? 'dark' : 'light';
    }
    return mode;
  }

  function getStoredThemeMode() {
    try {
      return sanitizeThemeMode(localStorage.getItem(themeStorageKey));
    } catch {
      return defaultThemeMode;
    }
  }

  function saveThemeMode(mode) {
    try {
      localStorage.setItem(themeStorageKey, mode);
    } catch {
    }
  }

  function updateThemeToggle(mode) {
    if (themeToggleLabel) {
      themeToggleLabel.textContent = `${themeModeLabels[mode] ?? mode}`;
    }

    if (themeToggleIcon) {
      themeToggleIcon.className = 'bi';
      if (mode === 'light') {
        themeToggleIcon.classList.add('bi-sun-fill');
      } else if (mode === 'dark') {
        themeToggleIcon.classList.add('bi-moon-stars-fill');
      } else {
        themeToggleIcon.classList.add('bi-circle-half');
      }
    }

    themeModeButtons.forEach(button => {
      const isActive = button.dataset.lunetThemeChoice === mode;
      button.classList.toggle('active', isActive);
      button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });
  }

  function applyThemeMode(mode, persist) {
    const sanitizedMode = sanitizeThemeMode(mode);
    const resolvedTheme = resolveThemeMode(sanitizedMode);
    rootElement.setAttribute('data-lunet-theme-mode', sanitizedMode);
    rootElement.setAttribute('data-bs-theme', resolvedTheme);
    if (persist) {
      saveThemeMode(sanitizedMode);
    }
    updateThemeToggle(sanitizedMode);
  }

  applyThemeMode(getStoredThemeMode(), false);

  themeModeButtons.forEach(button => {
    button.addEventListener('click', () => {
      applyThemeMode(button.dataset.lunetThemeChoice, true);
    });
  });

  const handleSystemThemeChange = () => {
    if ((rootElement.getAttribute('data-lunet-theme-mode') || defaultThemeMode) === 'system') {
      applyThemeMode('system', false);
    }
  };

  if (typeof themeMediaQuery.addEventListener === 'function') {
    themeMediaQuery.addEventListener('change', handleSystemThemeChange);
  } else if (typeof themeMediaQuery.addListener === 'function') {
    themeMediaQuery.addListener(handleSystemThemeChange);
  }

  const copyTooltipScope = document.querySelector('section[data-copy-tooltip]');
  const copyTooltipText = copyTooltipScope?.dataset.copyTooltip || 'Copy to Clipboard.';
  const copyTooltipSuccessText = copyTooltipScope?.dataset.copyTooltipSuccess || 'Copied!';
  const copyTooltipErrorText = copyTooltipScope?.dataset.copyTooltipError || 'Failed to copy!';

  const copyBtns = document.querySelectorAll('button.copy-to-clipboard-button');

  copyBtns.forEach(btn => {
    // Ensure tooltip attributes exist
    if (!btn.hasAttribute('data-bs-toggle')) btn.setAttribute('data-bs-toggle', 'tooltip');
    if (!btn.hasAttribute('data-bs-placement')) btn.setAttribute('data-bs-placement', 'left');
    if (!btn.hasAttribute('data-bs-title')) btn.setAttribute('data-bs-title', copyTooltipText);

    const tt = bootstrap.Tooltip.getOrCreateInstance(btn);
    const original = btn.getAttribute('data-bs-title') || copyTooltipText;

    // Hide tooltip on click (if visible from hover)
    //btn.addEventListener('click', () => tt.hide());

    // React to Prism's data-copy-state changes
    const obs = new MutationObserver(muts => {
      for (const m of muts) {
        if (m.type === 'attributes' && m.attributeName === 'data-copy-state') {
          const state = btn.getAttribute('data-copy-state');

          if (state === 'copy-success') {
            if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': copyTooltipSuccessText });
            else btn.setAttribute('data-bs-title', copyTooltipSuccessText);
            tt.show();
            // Drop focus so the button doesn’t stay highlighted
            btn.blur();
            setTimeout(() => {
              if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': original });
              else btn.setAttribute('data-bs-title', original);
              tt.hide();
            }, 1200);
          } else if (state === 'copy-error') {
            if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': copyTooltipErrorText });
            else btn.setAttribute('data-bs-title', copyTooltipErrorText);
            tt.show();
            // Drop focus so the button doesn’t stay highlighted
            btn.blur();
            setTimeout(() => {
              if (typeof tt.setContent === 'function') tt.setContent({ '.tooltip-inner': original });
              else btn.setAttribute('data-bs-title', original);
              tt.hide();
            }, 1500);
          }
        }
      }
    });
    obs.observe(btn, { attributes: true, attributeFilter: ['data-copy-state'] });
  });

  function wrapTerminalElement(element) {
      if (!element || element.closest('.terminal-window')) return;

      // Only wrap elements that are rendered as a "terminal screenshot" in docs.
      const tagName = element.tagName?.toUpperCase();
      if (tagName !== 'IMG' && tagName !== 'SVG' && tagName !== 'PRE' && tagName !== 'VIDEO') return;

      const titleText =
          element.getAttribute('data-terminal-title') ||
          element.getAttribute('title') ||
          element.getAttribute('alt') ||
          '';

      const wrapper = document.createElement('figure');
      wrapper.className = 'terminal-window';
      if (element.classList.contains('terminal-hero')) {
          wrapper.classList.add('terminal-window--hero');
      }

      const bar = document.createElement('div');
      bar.className = 'terminal-window__bar';

      const controls = document.createElement('div');
      controls.className = 'terminal-window__controls';

      const dotRed = document.createElement('span');
      dotRed.className = 'terminal-window__dot terminal-window__dot--red';
      const dotYellow = document.createElement('span');
      dotYellow.className = 'terminal-window__dot terminal-window__dot--yellow';
      const dotGreen = document.createElement('span');
      dotGreen.className = 'terminal-window__dot terminal-window__dot--green';
      controls.append(dotRed, dotYellow, dotGreen);

      const title = document.createElement('div');
      title.className = 'terminal-window__title';
      title.textContent = titleText;
      title.title = titleText;

      bar.append(controls, title);

      const content = document.createElement('div');
      content.className = 'terminal-window__content';

      const parent = element.parentNode;
      if (!parent) return;

      parent.insertBefore(wrapper, element);
      wrapper.append(bar, content);
      content.appendChild(element);
  }

  document.querySelectorAll('.terminal').forEach(wrapTerminalElement);

  function scrollSidebarToActiveItem(sidebar) {
      if (!sidebar || sidebar.scrollHeight <= sidebar.clientHeight) return;

      const activeItem = sidebar.querySelector('li.menu-item.active');
      if (!activeItem) return;

      const target = activeItem.querySelector('a.menu-link') || activeItem;
      requestAnimationFrame(() => {
          const sidebarRect = sidebar.getBoundingClientRect();
          const targetRect = target.getBoundingClientRect();
          const targetTopInSidebar = (targetRect.top - sidebarRect.top) + sidebar.scrollTop;
          const desiredTop = targetTopInSidebar - (sidebar.clientHeight / 2) + (targetRect.height / 2);
          const maxTop = sidebar.scrollHeight - sidebar.clientHeight;
          sidebar.scrollTop = Math.max(0, Math.min(desiredTop, maxTop));
      });
  }

  function expandMenuAncestors(sidebar) {
      if (!sidebar) return;

      sidebar.querySelectorAll('li.menu-item.active').forEach((activeItem) => {
          let collapseList = activeItem.closest('ol.collapse');
          while (collapseList) {
              collapseList.classList.add('show');
              if (collapseList.id) {
                  const toggles = sidebar.querySelectorAll(`a.menu-link-show[href='#${collapseList.id}']`);
                  toggles.forEach((toggle) => {
                      toggle.classList.remove('collapsed');
                      toggle.setAttribute('aria-expanded', 'true');
                  });
              }

              const parentItem = collapseList.closest('li.menu-item');
              collapseList = parentItem ? parentItem.closest('ol.collapse') : null;
          }
      });

      sidebar.querySelectorAll("a.menu-link-show[aria-expanded='true']").forEach((toggle) => {
          const href = toggle.getAttribute('href');
          if (!href || !href.startsWith('#')) return;
          const collapseList = sidebar.querySelector(href);
          if (collapseList && collapseList.classList.contains('collapse')) {
              collapseList.classList.add('show');
          }
          toggle.classList.remove('collapsed');
      });
  }

  document.querySelectorAll('.menu-sidebar').forEach((sidebar) => {
      expandMenuAncestors(sidebar);
      scrollSidebarToActiveItem(sidebar);
  });
});

// Initialize bootstrap tooltips
const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))

var jstoc = document.getElementsByClassName("js-toc");
if (jstoc.length > 0)
{
    tocbot.init({
        // Where to render the table of contents.
        tocSelector: '.js-toc',
        // Where to grab the headings to build the table of contents.
        contentSelector: '.js-toc-content',
        // Which headings to grab inside of the contentSelector element.
        headingSelector: 'h2, h3, h4',
        collapseDepth: 3,
        // Ensure correct positioning
        hasInnerContainers: true,
    });
}

var searchInput = document.getElementById("search-input");
var searchMenu = document.getElementById("search-results");
if (searchInput && searchMenu) {
    const emptySearchMessage = searchInput.dataset.searchEmptyMessage || 'Enter words to search...';
    const noSearchResultsMessage = searchInput.dataset.searchNoResultsMessage || 'No results found.';

    // Enable deep-link anchors only when search is present
    anchors.add(`.${lunetCssPrefix}-docs h2`);

    // Container
    const container = searchInput.closest(`.${lunetCssPrefix}-search`) || searchInput.parentElement;

    // Bootstrap Dropdown controller (guard if Bootstrap JS not present)
    var dropdown = (window.bootstrap && bootstrap.Dropdown)
        ? new bootstrap.Dropdown(searchInput, { autoClose: 'outside', display: 'static' })
        : null;

    let activeIndex = -1;
    let items = [];
    let lastQueryId = 0;

    function clearMenu() {
        searchMenu.innerHTML = '';
        items = [];
        activeIndex = -1;
    }

    function showMenu() {
        if (dropdown) {
            if (!searchMenu.classList.contains('show')) dropdown.show();
        } else {
            searchMenu.classList.add('show');
        }
        searchInput.setAttribute('aria-expanded', 'true');
    }

    function hideMenu() {
        if (dropdown) {
            if (searchMenu.classList.contains('show')) dropdown.hide();
        } else {
            searchMenu.classList.remove('show');
        }
        searchInput.setAttribute('aria-expanded', 'false');
    }

    function setActive(index) {
        if (index < -1 || index >= items.length) return;
        if (activeIndex >= 0 && items[activeIndex]) items[activeIndex].classList.remove('active');
        activeIndex = index;
        if (activeIndex >= 0 && items[activeIndex]) {
            items[activeIndex].classList.add('active');
            items[activeIndex].scrollIntoView({ block: 'nearest' });
        }
    }

    function renderMessage(message) {
        clearMenu();
        const li = document.createElement('li');
        const span = document.createElement('span');
        span.className = 'dropdown-item-text text-body-secondary';
        span.textContent = message;
        li.appendChild(span);
        searchMenu.appendChild(li);
        showMenu();
    }

    function renderRows(rows) {
        clearMenu();
        if (!rows || rows.length === 0) {
            renderMessage(noSearchResultsMessage);
            return;
        }
        const frag = document.createDocumentFragment();
        rows.forEach((row, i) => {
            const li = document.createElement('li');
            li.innerHTML = `<a class="dropdown-item" href="${row.url}">
                              <div class="fw-semibold">${row.title}</div>
                              <div class="small text-body-secondary">${row.snippet}</div>
                            </a>`;
            li.addEventListener('mouseover', () => setActive(i));
            frag.appendChild(li);
        });
        searchMenu.appendChild(frag);
        items = Array.from(searchMenu.querySelectorAll('.dropdown-item'));
        setActive(-1);
        showMenu();
    }

    function performSearch(term) {
        const queryId = ++lastQueryId;
        const q = (term || '').trim();
        if (!q) {
            clearMenu();
            renderMessage(emptySearchMessage);
            return;
        }
        DefaultLunetSearch.query(q).then(rows => {
            if (queryId !== lastQueryId) return; // stale
            renderRows(rows);
        }).catch(() => {
            if (queryId !== lastQueryId) return;
            renderMessage(noSearchResultsMessage);
        });
    }

    // Events
    searchInput.addEventListener('input', () => {
        performSearch(searchInput.value);
    });

    searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (!searchMenu.classList.contains('show')) showMenu();
            setActive(Math.min(activeIndex + 1, items.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setActive(Math.max(activeIndex - 1, -1));
        } else if (e.key === 'Enter') {
            if (activeIndex >= 0 && items[activeIndex]) {
                e.preventDefault();
                items[activeIndex].click();
            }
        } else if (e.key === 'Escape') {
            hideMenu();
        }
    });

    // Alt+S to focus
    document.addEventListener('keydown', (e) => {
        if (e.altKey && (e.key === 's' || e.key === 'S')) {
            searchInput.focus();
            if (searchInput.value && !searchMenu.classList.contains('show')) showMenu();
            e.preventDefault();
        }
    });

    // Show helper on focus
    searchInput.addEventListener('focus', () => {
        if (!searchInput.value) renderMessage(emptySearchMessage);
        else showMenu();
    });

    // Click-away handling if Bootstrap isn't managing it
    if (!dropdown) {
        document.addEventListener('click', (e) => {
            if (container && !container.contains(e.target)) hideMenu();
        });
    }
}

