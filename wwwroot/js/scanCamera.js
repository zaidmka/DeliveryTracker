// Loads html5-qrcode on demand, then scans using the rear camera when available.
// CDN dependency: for fully offline use, download html5-qrcode.min.js into wwwroot/js and reference it locally.
window.deliveryQrScanner = (() => {
    let scanner = null;
    let stopping = null;
    let scriptPromise = null;
    let scanned = false;

    function loadLibrary() {
        if (window.Html5Qrcode) return Promise.resolve();
        if (!scriptPromise) {
            scriptPromise = new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/html5-qrcode@2.3.8/html5-qrcode.min.js';
                script.onload = () => window.Html5Qrcode ? resolve() : reject(new Error('QR library unavailable'));
                script.onerror = () => reject(new Error('Failed to load QR library'));
                document.head.appendChild(script);
            }).catch(err => { scriptPromise = null; throw err; });
        }
        return scriptPromise;
    }

    async function stop() {
        if (stopping) return stopping;
        if (!scanner) return;
        const active = scanner;
        scanner = null;
        stopping = (async () => {
            try { await active.stop(); } catch (_) { /* Already stopped */ }
            try { active.clear(); } catch (_) { /* DOM may already be gone */ }
        })();
        try { await stopping; } finally { stopping = null; }
    }

    async function start(elementId, dotNetRef) {
        await stop();
        scanned = false;
        await loadLibrary();
        if (!document.getElementById(elementId)) throw new Error('Scanner element not found');
        scanner = new window.Html5Qrcode(elementId, { verbose: false });
        try {
            await scanner.start(
                { facingMode: 'environment' },
                { fps: 10, qrbox: (w, h) => { const s = Math.min(240, w - 24, h - 24); return { width: Math.max(100, s), height: Math.max(100, s) }; } },
                async decodedText => {
                    if (scanned) return;
                    scanned = true;
                    try { await dotNetRef.invokeMethodAsync('OnQrDetected', decodedText); }
                    catch (e) { console.error('QR callback failed', e); }
                    finally { scanned = false; }
                },
                () => { }
            );
        } catch (e) {
            await stop();
            throw e;
        }
    }

    return { start, stop };
})();
