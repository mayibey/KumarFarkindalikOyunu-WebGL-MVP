mergeInto(LibraryManager.library, {

    $PanelBridge: {
        listenerKuruldu: false,
        mesajListenerKur: function() {
            if (PanelBridge.listenerKuruldu) return;
            PanelBridge.listenerKuruldu = true;
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
    }

});
