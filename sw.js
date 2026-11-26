const CACHE = 'thebox-v6';

const RECURSOS = [
    './',
    './index.html',
    './auto.png',
    './banner.jfif',
    './Inicio.html',
    './LogIn.html',
    './MisPolizas.html',
    './MisPagos.html',
    './MisSiniestros.html',
    './MiPerfil.html',
    './Password.html',
    './Presupuesto.html',
    './manifest.json',
    './pwa.js',
    './demo.js',
    './documentos.js',
    './vendor/jspdf.umd.min.js',
    './vendor/jszip.min.js',
    './icons/icon-192.png',
    './icons/icon-512.png'
];

self.addEventListener('install', evento => {
    evento.waitUntil(
        caches.open(CACHE)
            .then(c => c.addAll(RECURSOS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', evento => {
    evento.waitUntil(
        caches.keys()
            .then(claves => Promise.all(claves.filter(c => c !== CACHE).map(c => caches.delete(c))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', evento => {
    const req = evento.request;
    if (req.method !== 'GET') return;

    const url = new URL(req.url);
    if (url.origin !== self.location.origin || url.pathname.toLowerCase().startsWith('/api/')) return;

    evento.respondWith(
        caches.match(req).then(cached => {
            if (cached) return cached;
            return fetch(req).then(res => {
                if (res.ok) {
                    const copia = res.clone();
                    caches.open(CACHE).then(c => c.put(req, copia));
                }
                return res;
            }).catch(() => caches.match('./Inicio.html'));
        })
    );
});
