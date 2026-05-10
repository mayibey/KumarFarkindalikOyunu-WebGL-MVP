// Tutorial sırasında panel.html iframe overlay'ini SOL kenara alır + SABİT boyut verir
// + KAPATMA YOLLARINI BLOKE EDER (4 kanal: outside click, X butonu, otomatik setTimeout, iç kapanmaTimer).
//
// PanelBridge.jslib (PaneliAc) iframe'i yarattıktan sonra çağrılır;
// 03_SenaryoluOyun'da ASLA çağrılmaz (TutorialOyunYoneticisi sadece build idx 3'te aktif),
// yani 03'teki davranış değişmez (panel.html orijinal full-screen + tüm kapatma yolları aktif kalır).

mergeInto(LibraryManager.library, {

    PaneliSolaAl: function() {
        var ov = document.getElementById('panelOverlay');
        var iframe = document.getElementById('panelIframe');
        if (!ov || !iframe) return;

        // === Sol konum + sabit boyut ===
        ov.style.justifyContent = 'flex-start';
        ov.style.alignItems = 'center';
        ov.style.paddingLeft = '20px';
        ov.style.background = 'rgba(0,0,0,0.5)';
        iframe.style.width = '540px';
        iframe.style.minWidth = '540px';
        iframe.style.maxWidth = '540px';
        iframe.style.height = '800px';
        iframe.style.minHeight = '800px';
        iframe.style.maxHeight = '800px';

        // === Katman 1: Backdrop click engelle (Kanal 1) ===
        // PanelBridge.jslib overlay click listener'ı yine çalışır ama event hiç gelmez
        // (pointer-events:none). iframe içi tıklamalar etkilenmez (auto).
        ov.style.pointerEvents = 'none';
        iframe.style.pointerEvents = 'auto';

        // === Katman 2 + 3: iframe içi override (X gizle + paneliKapat no-op) ===
        // iframe.load event'i (StreamingAssets same-origin → contentDocument erişimi OK)
        iframe.addEventListener('load', function() {
            try {
                var doc = iframe.contentDocument;
                var win = iframe.contentWindow;
                if (!doc || !win) return;

                // Katman 2: X (close) butonu gizle (Kanal 2)
                var closeBtn = doc.querySelector('.close-btn');
                if (closeBtn) closeBtn.style.display = 'none';

                // Katman 3: paneliKapat global no-op (Kanal 3 + 4: otomatik setTimeout + iç kapanmaTimer)
                win.paneliKapat = function() {
                    console.log('[Tutorial] panel.html paneliKapat çağrıldı, engellendi');
                };
            } catch (e) {
                console.warn('[TutorialPanelKonum] iframe override fail (load):', e);
            }
        });

        // === FALLBACK: Polling ===
        // iframe PaneliSolaAl çağrısından ÖNCE yüklenmişse (cache vs.) load event yeniden tetiklenmez.
        // 100 ms × 20 = 2 sn polling, contentDocument hazır olunca override uygulanır. Idempotent
        // (paneliKapat zaten "engellendi" içeriyorsa tekrar override etmez).
        var deneme = 0;
        var poll = setInterval(function() {
            if (deneme++ > 20) { clearInterval(poll); return; }
            try {
                var doc = iframe.contentDocument;
                var win = iframe.contentWindow;
                if (!doc || !win || !doc.body) return;

                var closeBtn = doc.querySelector('.close-btn');
                if (closeBtn && closeBtn.style.display !== 'none') {
                    closeBtn.style.display = 'none';
                }
                if (win.paneliKapat && win.paneliKapat.toString().indexOf('engellendi') === -1) {
                    win.paneliKapat = function() {
                        console.log('[Tutorial] panel.html paneliKapat engellendi (poll)');
                    };
                }
                clearInterval(poll);
            } catch (e) { /* iframe henüz hazır değil, sonraki tick'te dene */ }
        }, 100);
    }

});
