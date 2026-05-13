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
                // HOTFIX: İlk vurgu elementine smooth scroll — accordion açılınca kullanıcı görür
                if (els.length > 0) {
                    setTimeout(function() {
                        try { els[0].scrollIntoView({behavior: 'smooth', block: 'center'}); } catch(e) {}
                    }, 300); // accordion açılma animasyonu (~250ms) sonrası scroll
                }
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
    },

    // === PAKET 14-FAZ7 (T6YO ters): Toggle ZORLA AÇ (active class ekle + label "Aktif" + Unity true bildir) ===
    ToggleAc: function(idPtr) {
        var iframe = document.getElementById('panelIframe');
        if (!iframe) { console.warn('[Tutorial] ToggleAc: panelIframe yok'); return; }
        var id = UTF8ToString(idPtr);
        var deneme = 0;
        var uygula = function() {
            try {
                var doc = iframe.contentDocument;
                var win = iframe.contentWindow;
                if (!doc || !win) return false;
                var el = doc.getElementById(id);
                if (!el) return false;
                el.classList.add('active');
                var labelId = id.replace('Toggle', 'Label');
                var lbl = doc.getElementById(labelId);
                if (lbl) lbl.textContent = 'Aktif';
                if (win.unityeGonder) {
                    var key = id.replace('Toggle', '');
                    win.unityeGonder(key, true);
                }
                console.log('[Tutorial] ToggleAc (force):', id, '→ class eklendi, label=Aktif, Unity true');
                return true;
            } catch (e) {
                console.warn('[Tutorial] ToggleAc hata:', e);
                return false;
            }
        };
        if (!uygula()) {
            var poll = setInterval(function() {
                if (deneme++ > 10 || uygula()) clearInterval(poll);
            }, 100);
        }
    },

    // === HOTFIX2 (T6YO): Panel.html toggle elementini ZORLA KAPAT (3 katmanlı senkron) ===
    // Önceki el.click() programatik tıklama timing/onclick attribute uyumsuzluk olabiliyordu.
    // Bu versiyon DOĞRUDAN class kaldırır + label senkron + unityeGonder false. Plus retry pattern.
    ToggleKapat: function(idPtr) {
        var iframe = document.getElementById('panelIframe');
        if (!iframe) { console.warn('[Tutorial] ToggleKapat: panelIframe yok'); return; }
        var id = UTF8ToString(idPtr);
        var deneme = 0;
        var uygula = function() {
            try {
                var doc = iframe.contentDocument;
                var win = iframe.contentWindow;
                if (!doc || !win) return false;
                var el = doc.getElementById(id);
                if (!el) return false;

                // 1. Direkt class kaldır (toggleDegisti'yi tetiklemeden — yarış riski sıfır)
                el.classList.remove('active');

                // 2. Label senkron ("Aktif" → "Kapalı")
                var labelId = id.replace('Toggle', 'Label');
                var lbl = doc.getElementById(labelId);
                if (lbl) lbl.textContent = 'Kapalı';

                // 3. Unity'ye direkt false bildir (PanelKopru.yeniOyuncuModu set + OyunYoneticisi davranış kapanır)
                if (win.unityeGonder) {
                    var key = id.replace('Toggle', '');
                    win.unityeGonder(key, false);
                }

                console.log('[Tutorial] ToggleKapat (force):', id, '→ class kaldırıldı, label=Kapalı, Unity false');
                return true;
            } catch (e) {
                console.warn('[Tutorial] ToggleKapat hata:', e);
                return false;
            }
        };

        if (!uygula()) {
            // Retry: iframe henüz yüklenmemiş olabilir, 100ms aralık 10 deneme
            var poll = setInterval(function() {
                if (deneme++ > 10 || uygula()) clearInterval(poll);
            }, 100);
        }
    },

    // === Dropdown auto-revert: Uygula basılmadan blur/focus loss olursa eski değere geri dön ===
    // panel.html: <select id="oyunModu"> + <button id="senaryoUygulaBtn">. Tutorial kullanıcısı yanlışlıkla
    // dropdown'u değiştirip Uygula'ya basmadan tıklamadan kaçarsa, seçim sıfırlanır → kafa karışıklığı yok.
    DropdownAutoRevertEkle: function() {
        var iframe = document.getElementById('panelIframe');
        if (!iframe) return;

        var uygula = function() {
            try {
                var doc = iframe.contentDocument;
                if (!doc) return false;
                var sel = doc.getElementById('oyunModu');
                if (!sel) return false;
                var uygulaBtn = doc.getElementById('senaryoUygulaBtn');
                if (!uygulaBtn) return false;
                // Idempotent: aynı select'e iki kez kurulmasın
                if (sel.getAttribute('data-tutorial-revert') === '1') return true;
                sel.setAttribute('data-tutorial-revert', '1');

                var sonUygulanan = sel.value; // başlangıç değeri
                var bekliyor = false;

                uygulaBtn.addEventListener('click', function() {
                    sonUygulanan = sel.value;
                    bekliyor = false;
                    console.log('[Tutorial] Senaryo uygulandı:', sonUygulanan);
                });

                sel.addEventListener('change', function() {
                    if (sel.value !== sonUygulanan) bekliyor = true;
                });

                // HOTFIX: blur Uygula click'ten ÖNCE tetikleniyordu → revert kullanıcının seçimini siliyordu
                // → toast "normal modu uygulandı" görünüyordu. 200ms setTimeout ile Uygula click önce işlenir,
                // sonUygulanan güncellenir + bekliyor=false → revert IPTAL OLUR.
                sel.addEventListener('blur', function() {
                    setTimeout(function() {
                        if (bekliyor) {
                            sel.value = sonUygulanan;
                            bekliyor = false;
                            console.log('[Tutorial] Dropdown revert ->', sonUygulanan);
                        }
                    }, 200);
                });

                console.log('[Tutorial] Dropdown auto-revert kuruldu, baslangic:', sonUygulanan);
                return true;
            } catch (e) {
                console.warn('[Tutorial] DropdownAutoRevertEkle hata:', e);
                return false;
            }
        };

        iframe.addEventListener('load', uygula);
        var deneme = 0;
        var poll = setInterval(function() {
            if (deneme++ > 20 || uygula()) clearInterval(poll);
        }, 100);
    }

});
