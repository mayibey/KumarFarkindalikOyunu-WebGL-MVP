// Tutorial sırasında panel.html iframe yerleşim + boyut + kapatma engellemesi + parametre vurgu.
// PanelBridge.jslib (PaneliAc) iframe'i yarattıktan sonra çağrılır; 03_SenaryoluOyun'da ASLA çağrılmaz
// (TutorialOyunYoneticisi sadece build idx 3'te aktif).

mergeInto(LibraryManager.library, {

    PaneliSolaAl: function() {
        var ov = document.getElementById('panelOverlay');
        var iframe = document.getElementById('panelIframe');
        if (!ov || !iframe) return;

        // Sol konum + sabit boyut + transparent backdrop
        ov.style.justifyContent = 'flex-start';
        ov.style.alignItems = 'center';
        ov.style.paddingLeft = '0px'; // Panel ekran sol kenarına yapışır
        ov.style.background = 'transparent';
        iframe.style.width = '520px';
        iframe.style.minWidth = '520px';
        iframe.style.maxWidth = '520px';
        iframe.style.height = '720px';
        iframe.style.minHeight = '720px';
        iframe.style.maxHeight = '720px';

        // Katman 1: Backdrop click engelle
        ov.style.pointerEvents = 'none';
        iframe.style.pointerEvents = 'auto';

        // Katman 2+3: iframe içi (load + polling fallback)
        var uygula = function() {
            try {
                var doc = iframe.contentDocument;
                var win = iframe.contentWindow;
                if (!doc || !win || !doc.body) return false;
                var closeBtn = doc.querySelector('.close-btn');
                if (closeBtn && closeBtn.style.display !== 'none') closeBtn.style.display = 'none';
                if (win.paneliKapat && win.paneliKapat.toString().indexOf('engellendi') === -1) {
                    win.paneliKapat = function() {
                        console.log('[Tutorial] panel.html paneliKapat engellendi');
                    };
                }
                return true;
            } catch (e) { return false; }
        };
        iframe.addEventListener('load', uygula);
        var deneme = 0;
        var poll = setInterval(function() {
            if (deneme++ > 20 || uygula()) clearInterval(poll);
        }, 100);
    },

    // === Oyun modu dropdown — panel.html'deki <option>'lara title attribute ekle (native browser tooltip) ===

    DropdownTooltipEkle: function() {
        var iframe = document.getElementById('panelIframe');
        if (!iframe) return;

        var titles = {
            'normal': 'Manipülasyon kapalı, oyun kendi kurallarında akar (RTP %94).',
            'hook':   'Yeni oyuncuyu çekmek için: bol kazandırma, yumuşak kayıplar.',
            'yontma': 'Oyuncu farkına varmadan azar azar kaybettirme.',
            'tutma':  'Oyuncu çıkmaya niyetlenince küçük kazanç hediyesi ile tutar.',
            'koruma': 'Ödeme neredeyse durur, bakiye tüketilir.'
        };

        var uygula = function() {
            try {
                var doc = iframe.contentDocument;
                if (!doc) return false;
                var sel = doc.getElementById('oyunModu');
                if (!sel) return false;
                var optionlar = sel.querySelectorAll('option');
                if (optionlar.length === 0) return false;
                optionlar.forEach(function(o) {
                    if (titles[o.value]) o.title = titles[o.value];
                });
                return true;
            } catch (e) { return false; }
        };

        iframe.addEventListener('load', uygula);
        var deneme = 0;
        var poll = setInterval(function() {
            if (deneme++ > 20 || uygula()) clearInterval(poll);
        }, 100);
    },

    // === Tutorial vurgu — panel.html .apply-btn-pulsing class'ını ekle/çıkar + accordion auto-open ===

    VurguAc: function(selectorPtr) {
        var sel = UTF8ToString(selectorPtr);
        var iframe = document.getElementById('panelIframe');
        if (!iframe) return;
        var uygula = function() {
            try {
                var doc = iframe.contentDocument;
                if (!doc) return false;
                var els = doc.querySelectorAll(sel);
                if (els.length === 0) return false;
                els.forEach(function(el) {
                    // Accordion auto-open: el'in parent accordion-section'u kapalıysa header'a click
                    var acc = el.closest ? el.closest('.accordion-section') : null;
                    if (acc && acc.getAttribute('data-open') !== 'true') {
                        var header = acc.querySelector('.accordion-header');
                        if (header) header.click();
                    }
                    el.classList.add('apply-btn-pulsing');
                });
                return true;
            } catch (e) { return false; }
        };
        // Direkt + 1 tick gecikme — iframe henüz hazır değilse
        if (!uygula()) setTimeout(uygula, 200);
    },

    VurguKapat: function(selectorPtr) {
        var sel = UTF8ToString(selectorPtr);
        var iframe = document.getElementById('panelIframe');
        if (!iframe) return;
        try {
            var doc = iframe.contentDocument;
            if (!doc) return;
            doc.querySelectorAll(sel).forEach(function(el) {
                el.classList.remove('apply-btn-pulsing');
            });
        } catch (e) {}
    },

    TumVurgulariKapat: function() {
        var iframe = document.getElementById('panelIframe');
        if (!iframe) return;
        try {
            var doc = iframe.contentDocument;
            if (!doc) return;
            doc.querySelectorAll('.apply-btn-pulsing').forEach(function(el) {
                el.classList.remove('apply-btn-pulsing');
            });
        } catch (e) {}
    },

    // === T_SON sonrası panel.html iframe'i kapat (overlay remove). PaneliKapat alias (TutorialPaneliKapat) ===
    TutorialPaneliKapat: function() {
        var ov = document.getElementById('panelOverlay');
        if (ov) ov.remove();
    }

});
