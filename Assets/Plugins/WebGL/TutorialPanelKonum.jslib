// Tutorial sırasında panel.html iframe overlay'ini SOL kenara alır + SABİT boyut verir.
// PanelBridge.jslib (PaneliAc) iframe'i yarattıktan sonra çağrılır;
// 03_SenaryoluOyun'da çağrılmaz (TutorialOyunYoneticisi sadece build idx 3'te aktif).
// Bu sayede 03'teki panel.html iframe ortada ve dinamik açılmaya devam eder, sadece 04'te
// sola + sabit (540 × 800) yerleştirilir.

mergeInto(LibraryManager.library, {

    PaneliSolaAl: function() {
        var ov = document.getElementById('panelOverlay');
        var iframe = document.getElementById('panelIframe');
        if (!ov || !iframe) return;

        // Overlay: sola hizala + dikey ortala + yarı saydam siyah backdrop
        ov.style.justifyContent = 'flex-start';
        ov.style.alignItems = 'center';
        ov.style.paddingLeft = '20px';
        ov.style.background = 'rgba(0,0,0,0.5)';

        // Iframe: SABİT 540 × 800 (viewport değişiminden bağımsız)
        iframe.style.width = '540px';
        iframe.style.minWidth = '540px';
        iframe.style.maxWidth = '540px';
        iframe.style.height = '800px';
        iframe.style.minHeight = '800px';
        iframe.style.maxHeight = '800px';
    }

});
