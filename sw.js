// Minimal service worker for offline-first shell
const CACHE_NAME = 'ai-mate-pwa-v2';
const ASSETS = [
  '/',
  '/index.html',
  '/manifest.webmanifest',
  '/src/styles.css',
  '/src/swRegistration.js',
  '/Brand.png',
  // Icons (add when available)
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/icons/maskable-icon-192.png',
  '/icons/maskable-icon-512.png',
  // Pages
  '/pages/home.html',
  '/pages/jobs.html',
  '/pages/quotes.html',
  '/pages/invoices.html',
  '/pages/expenses.html',
  '/pages/clients.html',
  '/pages/assistant.html',
  '/pages/settings.html'
];

self.addEventListener('install', (event) => {
  self.skipWaiting();
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS))
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  const { request } = event;
  // Network-first for pages; cache-first for static assets
  if (request.destination === 'document' || request.url.includes('/pages/')) {
    event.respondWith(
      fetch(request).then((res) => {
        const resClone = res.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(request, resClone));
        return res;
      }).catch(() => caches.match(request))
    );
  } else {
    event.respondWith(
      caches.match(request).then((cached) => cached || fetch(request))
    );
  }
});
