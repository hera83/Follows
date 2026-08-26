// Minimal service worker for the installable PWA shell (see _AppShellLayout.cshtml, which registers
// this with { scope: '/app/' } so it can never intercept desktop-app requests).
//
// Scope deliberately kept small: this precaches only the shell's own static assets and an offline
// fallback page. It does NOT cache Feed/Documents/Profil HTML or JSON — that's all authenticated,
// per-viewer, constantly-changing data; caching it for offline viewing is a meaningfully bigger
// feature (cache invalidation, per-user storage) left for a later pass.

const CACHE_NAME = 'follows-pwa-shell-v1';
const OFFLINE_URL = '/offline.html';
const PRECACHE_URLS = [
    OFFLINE_URL,
    '/css/pwa-shell.css',
    '/js/pwa-shell.js',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const { request } = event;
    if (request.method !== 'GET') return;

    // Full-page navigations: network-first, offline page as last resort — never serve stale
    // authenticated HTML from the cache.
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match(OFFLINE_URL))
        );
        return;
    }

    // Same-origin precached shell assets only: cache-first, falling back to network.
    const url = new URL(request.url);
    if (url.origin === self.location.origin && PRECACHE_URLS.includes(url.pathname)) {
        event.respondWith(
            caches.match(request).then((cached) => cached || fetch(request))
        );
    }
});
