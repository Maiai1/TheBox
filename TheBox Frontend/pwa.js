// Habilita la PWA en todas las páginas del portal.
if (!document.querySelector('link[rel="manifest"]')) {
    const manifest = document.createElement('link');
    manifest.rel = 'manifest';
    manifest.href = 'manifest.json';
    document.head.append(manifest);
}

if (!document.querySelector('meta[name="theme-color"]')) {
    const tema = document.createElement('meta');
    tema.name = 'theme-color';
    tema.content = '#16325c';
    document.head.append(tema);
}

if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('sw.js').catch(() => {});
    });
}
