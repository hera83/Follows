(() => {
    // ─── Sidebar ─────────────────────────────────────────────────────────────
    const shell = document.querySelector('.app-shell');
    const toggle = document.getElementById('sidebarToggle');

    const applySidebarDefault = () => {
        if (!shell) return;
        shell.classList.toggle('sidebar-collapsed', localStorage.getItem('sidebar-collapsed') === 'true');
    };

    applySidebarDefault();

    if (toggle && shell) {
        toggle.addEventListener('click', () => {
            shell.classList.toggle('sidebar-collapsed');
            localStorage.setItem('sidebar-collapsed', shell.classList.contains('sidebar-collapsed').toString());
        });
    }

    // ─── Toasts ───────────────────────────────────────────────────────────────
    const initToasts = (container) => {
        (container || document).querySelectorAll('.fv-toast').forEach((el) => {
            const delay = parseInt(el.dataset.delay || '3000', 10);
            const closeBtn = el.querySelector('.fv-toast-close');

            const hide = () => {
                el.classList.add('fv-toast-hide');
                el.addEventListener('animationend', () => el.remove(), { once: true });
            };

            if (closeBtn) closeBtn.addEventListener('click', hide);
            setTimeout(hide, delay);
        });
    };

    const toastPresets = {
        success: { css: 'fv-toast-success', icon: 'bi-check-circle-fill', delay: 3000 },
        info: { css: 'fv-toast-info', icon: 'bi-info-circle-fill', delay: 3000 },
        warning: { css: 'fv-toast-warn', icon: 'bi-exclamation-triangle-fill', delay: 10000 },
        error: { css: 'fv-toast-error', icon: 'bi-x-circle-fill', delay: 10000 }
    };

    const showToast = (type, message) => {
        if (!message) return;
        const preset = toastPresets[type] || toastPresets.info;

        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            document.body.appendChild(container);
        }

        const el = document.createElement('div');
        el.className = `fv-toast ${preset.css}`;
        el.dataset.delay = String(preset.delay);
        el.innerHTML = `
            <i class="bi ${preset.icon} fv-toast-icon"></i>
            <span></span>
            <button class="fv-toast-close" type="button" aria-label="Luk"><i class="bi bi-x"></i></button>
        `;
        el.querySelector('span').textContent = message;
        container.appendChild(el);
        initToasts(container);
    };

    initToasts();
    window.FvToast = { show: showToast };

    // ─── Async form submission ─────────────────────────────────────────────────
    // Submits a form via fetch, shows the resulting toast and returns the parsed
    // { success, message, type } payload so callers can react (close modal, reload table, ...).
    const submitFormAjax = async (form) => {
        const formData = new FormData(form);
        try {
            const res = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'fetch' }
            });
            const data = await res.json();
            showToast(data.type || (data.success ? 'success' : 'error'), data.message);
            return data;
        } catch {
            showToast('error', 'Netværksfejl. Prøv igen.');
            return { success: false };
        }
    };

    window.FvForm = { submit: submitFormAjax };

    // ─── Async navigation ─────────────────────────────────────────────────────
    const contentArea = document.querySelector('.app-content');

    const setActiveLink = (url) => {
        const path = new URL(url, location.origin).pathname;
        document.querySelectorAll('.menu-item').forEach((link) => {
            if (link.tagName === 'A') {
                const linkPath = new URL(link.href, location.origin).pathname;
                link.classList.toggle('active', linkPath === path);
            }
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
                history.pushState({ ajaxNav: true, url }, '', url);
            }

            setActiveLink(url);
            initToasts(contentArea);
            window.FvDataTable?.initAll(contentArea);
            window.FvChat?.initAll(contentArea);

            // Re-execute inline scripts injected via innerHTML
            contentArea.querySelectorAll('script').forEach((oldScript) => {
                const newScript = document.createElement('script');
                Array.from(oldScript.attributes).forEach((attr) =>
                    newScript.setAttribute(attr.name, attr.value)
                );
                newScript.textContent = oldScript.textContent;
                oldScript.replaceWith(newScript);
            });
        } catch (err) {
            if (err.name !== 'AbortError') location.href = url;
        }
    };

    // Intercept sidebar link clicks
    document.addEventListener('click', (e) => {
        const link = e.target.closest('.menu-item');
        if (!link || link.tagName !== 'A') return;

        e.preventDefault();
        if (link.href === location.href) return;
        navigate(link.href, true);
    });

    // Handle browser back/forward
    window.addEventListener('popstate', (e) => {
        if (e.state?.ajaxNav) {
            navigate(e.state.url, false);
        } else {
            location.reload();
        }
    });

    // Set active state and seed history for initial load
    setActiveLink(location.href);
    history.replaceState({ ajaxNav: true, url: location.href }, document.title, location.href);
})();
