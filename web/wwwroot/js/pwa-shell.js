(() => {
    // Chrome specific to the installable PWA shell (_AppShellLayout.cshtml) — the collapsible menu
    // panel, and AJAX navigation kept inside the "/app/..." URL space. site.js is loaded alongside
    // this file for its (already null-safe) toast/form helpers, but its own menu-toggle and
    // ".app-content"/".menu-item" navigation logic never match anything on this page, since none of
    // those classes/ids exist here — this file only handles what's structurally different.
    const T = (key) => (window.i18n && window.i18n[key]) || key;

    const shell = document.getElementById('pwaShell');
    const toggle = document.getElementById('pwaMenuToggle');
    const backdrop = document.getElementById('pwaMenuBackdrop');
    const contentArea = document.querySelector('.pwa-content');
    const toggleIcon = toggle?.querySelector('i');

    // ─── Collapsible menu panel ─────────────────────────────────────────────
    const closeMenu = () => {
        if (!shell) return;
        shell.classList.remove('pwa-menu-open');
        toggle?.setAttribute('aria-expanded', 'false');
        toggle?.setAttribute('aria-label', T('Åbn menu'));
        toggleIcon?.classList.replace('bi-x-lg', 'bi-list');
    };

    const openMenu = () => {
        if (!shell) return;
        shell.classList.add('pwa-menu-open');
        toggle?.setAttribute('aria-expanded', 'true');
        toggle?.setAttribute('aria-label', T('Luk menu'));
        toggleIcon?.classList.replace('bi-list', 'bi-x-lg');
    };

    toggle?.addEventListener('click', () => {
        shell?.classList.contains('pwa-menu-open') ? closeMenu() : openMenu();
    });

    backdrop?.addEventListener('click', closeMenu);

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeMenu();
    });

    // ─── AJAX navigation, kept inside "/app/..." ────────────────────────────
    // Every link this shell ever renders (menu panel + Feed/Documents/Profile content, which are the
    // same views the desktop shell uses) points at the plain, unprefixed controller routes (e.g. the
    // "back to Documents" link, or a document-group card) — there is no separate "/app" route, see
    // PwaShellMiddleware. Rewriting every intercepted href to carry "/app" here, once, is what keeps
    // the whole session inside the installed-app shell without needing to touch any of those views.
    const toAppUrl = (href) => {
        let url;
        try {
            url = new URL(href, location.href);
        } catch {
            return null;
        }
        if (url.origin !== location.origin) return null;
        if (url.pathname !== '/app' && !url.pathname.startsWith('/app/')) {
            url.pathname = '/app' + url.pathname;
        }
        return url;
    };

    const setActiveLink = (pathname) => {
        document.querySelectorAll('.pwa-nav-link').forEach((link) => {
            if (link.tagName !== 'A') return;
            const linkUrl = toAppUrl(link.getAttribute('href'));
            link.classList.toggle('active', !!linkUrl && linkUrl.pathname === pathname);
        });
    };

    let currentController = null;

    const navigate = async (url, pushToHistory) => {
        if (!contentArea) return;

        if (currentController) currentController.abort();
        currentController = new AbortController();

        try {
            const response = await fetch(url, {
                headers: { 'X-Ajax-Navigation': 'true' },
                signal: currentController.signal
            });

            if (!response.ok) {
                location.href = url;
                return;
            }

            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const wrapper = doc.querySelector('[data-ajax-content]');

            if (!wrapper) {
                location.href = url;
                return;
            }

            contentArea.innerHTML = wrapper.innerHTML;

            if (pushToHistory) {
                history.pushState({ pwaNav: true, url }, '', url);
            }

            setActiveLink(new URL(url, location.origin).pathname);
            window.FvDataTable?.initAll(contentArea);
            window.FvChat?.initAll(contentArea);

            // Re-execute inline scripts injected via innerHTML (toast init included, since the
            // ajax-content wrapper itself carries the toast partial).
            contentArea.querySelectorAll('script').forEach((oldScript) => {
                const newScript = document.createElement('script');
                Array.from(oldScript.attributes).forEach((attr) =>
                    newScript.setAttribute(attr.name, attr.value)
                );
                newScript.textContent = oldScript.textContent;
                oldScript.replaceWith(newScript);
            });

            contentArea.scrollTo({ top: 0 });
        } catch (err) {
            if (err.name !== 'AbortError') location.href = url;
        }
    };

    document.addEventListener('click', (e) => {
        const link = e.target.closest('.pwa-menu-panel a[href], .pwa-content a[href]');
        if (!link) return;

        // Let modal triggers, external/anchor/mailto/tel links and deliberate new-tab clicks behave
        // normally — only intercept genuine same-app navigations.
        if (link.dataset.bsToggle || link.target === '_blank' || link.hasAttribute('download')) return;
        if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;

        const appUrl = toAppUrl(link.getAttribute('href'));
        if (!appUrl) return;

        e.preventDefault();
        closeMenu();
        if (appUrl.href === location.href) return;
        navigate(appUrl.href, true);
    });

    window.addEventListener('popstate', (e) => {
        if (e.state?.pwaNav) {
            navigate(e.state.url, false);
        } else {
            location.reload();
        }
    });

    setActiveLink(location.pathname);
    history.replaceState({ pwaNav: true, url: location.href }, document.title, location.href);
})();
