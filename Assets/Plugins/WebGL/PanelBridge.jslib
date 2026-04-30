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
        container.style.cssText = 'position:fixed;top:80px;left:12px;width:320px;z-index:5000;pointer-events:auto;';

        var iframe = document.createElement('iframe');
        iframe.id = 'anlaticiPanelIframe';
        iframe.src = url;
        iframe.style.cssText = 'width:100%;height:600px;border:none;background:transparent;';
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
    }

});
