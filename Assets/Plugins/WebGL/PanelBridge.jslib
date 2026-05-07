mergeInto(LibraryManager.library, {

    $PanelBridge: {
        listenerKuruldu: false,
        mesajListenerKur: function() {
            if (PanelBridge.listenerKuruldu) return;
            PanelBridge.listenerKuruldu = true;

            // 1. Yönetici Panel mesajları (admin panel.html'den)
            window.addEventListener('message', function(e) {
                var msg = e.data;
                if (!msg || msg.source !== 'yoneticiPanel') return;
                var json = JSON.stringify({
                    source: msg.source,
                    key:    msg.key,
                    value:  String(msg.value)
                });
                if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
                    unityInstance.SendMessage('PanelKopru', 'AyarAl', json);
                }
                if (msg.key === 'paneliKapat') {
                    var ov = document.getElementById('panelOverlay');
                    if (ov) ov.remove();
                    var bo = document.getElementById('bahisPanelOverlay');
                    if (bo) bo.remove();
                }
            }, false);

            // 2. Bahis Sec HTML'den gelen resize/ready mesajları
            window.addEventListener('message', function(e) {
                var msg = e.data;
                if (!msg || msg.source !== 'bahisSecHtml') return;
                var iframe = document.getElementById('bahisPanelIframe');
                if (!iframe) return;

                if (msg.type === 'resize' && msg.height) {
                    iframe.style.height = (msg.height + 8) + 'px'; // 8px tampon
                }

                if (msg.type === 'ready') {
                    if (typeof window._sonBahisBakiye !== 'undefined' && iframe.contentWindow) {
                        iframe.contentWindow.postMessage({
                            source: 'unityToBahis',
                            bakiye: window._sonBahisBakiye
                        }, '*');
                    }
                }
            }, false);

            // 3. Anlatıcı Şerit HTML'den gelen resize/ready mesajları
            window.addEventListener('message', function(e) {
                var msg = e.data;
                if (!msg || msg.source !== 'anlaticiHtml') return;
                var iframe = document.getElementById('anlaticiPanelIframe');
                if (!iframe) return;

                if (msg.type === 'resize' && msg.height) {
                    iframe.style.height = (msg.height + 8) + 'px';
                }
                if (msg.type === 'ready') {
                    if (typeof window._sonAnlaticiState !== 'undefined' && iframe.contentWindow) {
                        var st = window._sonAnlaticiState;
                        st.source = 'unityToAnlatici';
                        iframe.contentWindow.postMessage(st, '*');
                    }
                }
            }, false);
        }
    },

    PaneliAc__deps: ['$PanelBridge'],
    PaneliAc: function(urlPtr) {
        PanelBridge.mesajListenerKur();

        var url = UTF8ToString(urlPtr);
        if (document.getElementById('panelIframe')) return;

        var overlay = document.createElement('div');
        overlay.id = 'panelOverlay';
        overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.7);z-index:9998;display:flex;align-items:center;justify-content:center;';

        var iframe = document.createElement('iframe');
        iframe.id = 'panelIframe';
        iframe.src = url;
        iframe.style.cssText = 'border:none;width:98%;max-width:1850px;height:92vh;max-height:1000px;border-radius:12px;background:transparent;z-index:9999;';
        iframe.setAttribute('allowtransparency', 'true');

        overlay.appendChild(iframe);
        document.body.appendChild(overlay);

        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) overlay.remove();
        });
    },

    PaneliKapat: function() {
        var overlay = document.getElementById('panelOverlay');
        if (overlay) overlay.remove();
    },

    AyarlariPanelleGonder: function(jsonPtr) {
        var json = UTF8ToString(jsonPtr);
        var iframe = document.getElementById('panelIframe');
        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                source: 'unityToPanel',
                key:    'mevcutAyarlar',
                value:  json
            }, '*');
        } else {
            console.warn('[PanelBridge] AyarlariPanelleGonder: panelIframe bulunamadi');
        }
    },

    // ========== BAHİS PANEL (küçük merkezli pop-up) ==========
    BahisPaneliAc__deps: ['$PanelBridge'],
    BahisPaneliAc: function(urlPtr) {
        PanelBridge.mesajListenerKur();
        var url = UTF8ToString(urlPtr);

        var existing = document.getElementById('bahisPanelOverlay');
        if (existing) existing.remove();

        var overlay = document.createElement('div');
        overlay.id = 'bahisPanelOverlay';
        // align-items:flex-end + padding-bottom:140px — popup viewport altına yaslanır,
        // SPIN butonu ve alt UI şeridinin azıcık üstünde durur (slot ekran üst kısmı serbest kalır)
        overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.7);z-index:10000;display:flex;align-items:flex-end;justify-content:center;padding-bottom:140px;';

        var iframe = document.createElement('iframe');
        iframe.id = 'bahisPanelIframe';
        iframe.src = url;
        // Başlangıç height 480px; resize mesajı geldiğinde gerçek içerik boyutuna ayarlanır.
        iframe.style.cssText = 'width:min(540px, calc(100vw - 32px));height:480px;max-height:90vh;border:none;border-radius:14px;box-shadow:0 20px 60px rgba(0,0,0,0.6);background:transparent;z-index:10001;';
        iframe.setAttribute('allowtransparency', 'true');

        overlay.appendChild(iframe);
        document.body.appendChild(overlay);

        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) overlay.remove();
        });
    },

    BahisPaneliKapat: function() {
        var ov = document.getElementById('bahisPanelOverlay');
        if (ov) ov.remove();
        // Cached bakiyeyi temizle (sonraki açılışta stale değer kalmasın)
        window._sonBahisBakiye = undefined;
    },

    BahisPaneliBakiyeGonder: function(bakiye) {
        window._sonBahisBakiye = bakiye;
        var iframe = document.getElementById('bahisPanelIframe');
        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                source: 'unityToBahis',
                bakiye: bakiye
            }, '*');
        }
    },

    // ========== ANLATICI ŞERİT (sol persistent iframe) ==========
    AnlaticiPaneliAc__deps: ['$PanelBridge'],
    AnlaticiPaneliAc: function(urlPtr) {
        PanelBridge.mesajListenerKur();
        var url = UTF8ToString(urlPtr);

        var existing = document.getElementById('anlaticiPanelContainer');
        if (existing) existing.remove();

        var container = document.createElement('div');
        container.id = 'anlaticiPanelContainer';
        // Sabit konum + boyut: sol-orta, 360x700. z-index 100 = Unity canvas üstünde ama Unity Canvas
        // overlay'lerinin (modal 9998+, balon, yükleme) altında kalması için Gizle/Goster API ile
        // toggle edilir (display:none ↔ block) — modal/balon açılınca gizlenip kapanınca geri açılır.
        container.style.cssText = 'position:fixed;top:50%;left:20px;transform:translateY(-50%);width:420px;height:auto;min-height:600px;max-height:calc(100vh - 40px);overflow-y:auto;z-index:100;pointer-events:auto;';

        var iframe = document.createElement('iframe');
        iframe.id = 'anlaticiPanelIframe';
        iframe.src = url;
        iframe.style.cssText = 'width:100%;height:auto;min-height:600px;border:none;background:transparent;';
        iframe.setAttribute('allowtransparency', 'true');

        container.appendChild(iframe);
        document.body.appendChild(container);
    },

    AnlaticiPaneliKapat: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) c.remove();
        window._sonAnlaticiState = undefined;
    },

    AnlaticiPaneliGuncelle: function(jsonPtr) {
        var json = UTF8ToString(jsonPtr);
        try {
            var data = JSON.parse(json);
            window._sonAnlaticiState = data;
            var iframe = document.getElementById('anlaticiPanelIframe');
            if (iframe && iframe.contentWindow) {
                data.source = 'unityToAnlatici';
                iframe.contentWindow.postMessage(data, '*');
            }
        } catch(e) {
            console.warn('[AnlaticiPaneliGuncelle] JSON parse hatasi:', e);
        }
    },

    /// Modal/balon/yükleme paneli açılırken anlatici iframe'i gizler (Unity Canvas overlay'lerinin
    /// üstünde kalmaması için). Container DOM'da kalır, sadece display:none yapılır.
    AnlaticiPaneliGizle: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) c.style.display = 'none';
    },

    /// AnlaticiPaneliGizle ile gizlenen paneli geri açar.
    AnlaticiPaneliGoster: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) c.style.display = 'block';
    },

    /// Sol panel iframe'ini yarı saydam yapar (opacity:0.4) + tıklamayı yutmaz (pointer-events:none).
    /// z-index 100'de kalır — Unity canvas opaque siyah olduğundan alt katmana atmak işe yaramıyor.
    /// Modal pixels Unity canvas'ta, anlatici üstünde solgun → kullanıcı modal'ı net görür, panel
    /// arkaplandan okunmaya devam eder. Pre-A1 gibi modal "sol panel" anlattığı durumlarda kullanılır.
    AnlaticiPaneliArkayaAt: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) {
            c.style.opacity = '0.4';
            c.style.pointerEvents = 'none';
            console.log('[Panel] arka — opacity 0.4, pointer-events none');
        }
    },

    /// AnlaticiPaneliArkayaAt ile arkaya alınan paneli normal opaklığa geri döndürür.
    AnlaticiPaneliOneAl: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) {
            c.style.opacity = '1';
            c.style.pointerEvents = 'auto';
            console.log('[Panel] ön — opacity 1, pointer-events auto');
        }
    },

    // ========== HOŞGELDİN KUTUSU (sağ üst, sahne girişinde otomatik) ==========
    // Parametre: kullanıcı adı (KullaniciVerileri.KullaniciAdi). Boş/null gelirse "Misafir" fallback.
    HosgeldinKutusunuAc: function(adPtr) {
        var ad = UTF8ToString(adPtr);
        if (!ad || ad.trim() === '') ad = 'Misafir';

        var existing = document.getElementById('hosgeldinKutusu');
        if (existing) existing.remove();

        var box = document.createElement('div');
        box.id = 'hosgeldinKutusu';
        box.style.cssText = 'position:fixed;top:20px;right:20px;max-width:280px;padding:12px 16px;background:linear-gradient(135deg,#1a1f3a 0%,#2d3561 100%);border:1px solid #FFD700;border-radius:12px;box-shadow:0 4px 24px rgba(0,0,0,0.4);z-index:99;font-family:inherit;color:#FFFFFF;';

        var kapat = document.createElement('div');
        kapat.style.cssText = 'position:absolute;top:6px;right:10px;font-size:18px;color:#888;cursor:pointer;line-height:1;user-select:none;';
        kapat.textContent = '×';
        kapat.onmouseover = function() { kapat.style.color = '#FFFFFF'; };
        kapat.onmouseout  = function() { kapat.style.color = '#888'; };
        kapat.onclick     = function() { box.remove(); };

        var baslik = document.createElement('div');
        baslik.style.cssText = 'font-size:20px;font-weight:bold;color:#FFFFFF;padding-right:18px;';
        baslik.textContent = 'Hoş Geldiniz ' + ad;

        box.appendChild(kapat);
        box.appendChild(baslik);
        document.body.appendChild(box);
    },

    HosgeldinKutusunuKapat: function() {
        var b = document.getElementById('hosgeldinKutusu');
        if (b) b.remove();
    }

});
