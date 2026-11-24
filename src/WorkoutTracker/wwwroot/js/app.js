// Small helpers for phone/browser features called from C# via JS interop.
window.workoutApp = {
    buzz(pattern) {
        if (navigator.vibrate) navigator.vibrate(pattern);
    },

    restDone() {
        this.buzz([200, 100, 200]);
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            [0, 0.18].forEach(offset => {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.frequency.value = 880;
                gain.gain.setValueAtTime(0.0001, ctx.currentTime + offset);
                gain.gain.exponentialRampToValueAtTime(0.25, ctx.currentTime + offset + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + offset + 0.15);
                osc.connect(gain).connect(ctx.destination);
                osc.start(ctx.currentTime + offset);
                osc.stop(ctx.currentTime + offset + 0.16);
            });
            setTimeout(() => ctx.close(), 600);
        } catch { /* audio not available */ }
    },

    download(fileName, content, mime) {
        const url = URL.createObjectURL(new Blob([content], { type: mime }));
        const a = Object.assign(document.createElement('a'), { href: url, download: fileName });
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },

    setTheme(theme) {
        const root = document.documentElement;
        if (theme === 'system') delete root.dataset.theme; else root.dataset.theme = theme;
        const bg = getComputedStyle(root).getPropertyValue('--bg').trim();
        document.querySelector('meta[name="theme-color"]')?.setAttribute('content', bg || '#0a0b0f');
        try { localStorage.setItem('workout-tracker:theme', theme); } catch { }
    }
};
