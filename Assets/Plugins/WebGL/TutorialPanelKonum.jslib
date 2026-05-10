// Tutorial sırasında panel.html iframe overlay'ini SOL kenara alır.
// PanelBridge.jslib (PaneliAc) iframe'i yarattıktan sonra çağrılır;
// 03_SenaryoluOyun'da çağrılmaz (TutorialOyunYoneticisi sadece build idx 3'te aktif).
// Bu sayede 03'teki panel.html iframe ortada açılmaya devam eder, sadece 04'te sola alınır.

mergeInto(LibraryManager.library, {

    PaneliSolaAl: function() {
        var ov = document.getElementById('panelOverlay');
        var iframe = document.getElementById('panelIframe');
        if (!ov || !iframe) return;
        ov.style.justifyContent = 'flex-start';
        ov.style.paddingLeft = '20px';
        ov.style.background = 'rgba(0,0,0,0.5)'; // hafif şeffaflaştır (slot grid sağda görünsün)
        iframe.style.width = '540px';
        iframe.style.maxWidth = '540px';
    }

});
