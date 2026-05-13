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

                // Resize mesajı IGNORE edilir — container sabit yükseklikte (calc(100vh-280px)),
                // iframe height:100% ile container'ı doldurur. İçerik taşması iframe içi body
                // scroll ile çözülür. (Eski davranış: iframe.style.height dinamik → panel zıplardı.)
                // if (msg.type === 'resize' && msg.height) { /* artıkno-op */ }
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

    // PAKET 14-FAZ34 İş 6: Tutorial adım bazlı disabled — aktif kalacak selector listesini gönder.
    // panel.html message listener 'tutorialKilit' key'i için ayarlariKilitle() çağırır.
    TutorialPanelKilitGonderJslib: function(jsonPtr) {
        var json = UTF8ToString(jsonPtr);
        var iframe = document.getElementById('panelIframe');
        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                source: 'unityToPanel',
                key:    'tutorialKilit',
                value:  json
            }, '*');
        }
        // panel henüz açılmamış olabilir (T1, T2) — sessiz fail, T3'te panel açılınca tekrar gönderilebilir.
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
        // SABİT konum + boyut: top:200px (logo bandının altı), 460×calc(100vh-340px).
        // Üstten 200px (logo + nefes) + alttan 140px (bakiye/spin + nefes) = 340px toplam pay.
        // Aşamadan aşamaya panel yüksekliği DEĞİŞMEZ. overflow:hidden — iç scroll iframe body'sinde.
        // z-index 100 = Unity canvas üstünde; Gizle/Goster API ile modal/balon altında kalır.
        // transform:none + opacity:1 EXPLICIT → ArkayaAt sonrası state'i ilk render'da garanti sıfırla
        // (browser cache eski JSLIB tutsa bile yeni container default doğru başlar).
        // transition → ArkayaAt/OneAl çağrılarında transform/opacity yumuşak slide-out/in animasyonu.
        container.style.cssText = 'position:fixed;top:200px;left:20px;width:460px;height:calc(100vh - 340px);overflow:hidden;z-index:100;pointer-events:auto;transform:none;opacity:1;transition:transform 0.4s ease, opacity 0.4s ease;';

        var iframe = document.createElement('iframe');
        iframe.id = 'anlaticiPanelIframe';
        iframe.src = url;
        // Iframe container'ı tamamen doldurur; içerik fazlaysa iframe içindeki body scroll yapar.
        iframe.style.cssText = 'width:100%;height:100%;border:none;background:transparent;';
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

    /// Sol panel iframe'i TAMAMEN EKRAN DIŞINA kaydırır: transform:translateX(-500px) + opacity:0 + pointer-events:none.
    /// Panel sağ kenarı -20px, sol kenarı -480px — tamamen ekran dışı, karakter alanı tertemiz.
    /// Container cssText'inde transition tanımlı olduğu için yumuşak slide-out animasyonuyla çıkar.
    AnlaticiPaneliArkayaAt: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) {
            c.style.transform = 'translateX(-500px)';
            c.style.opacity = '0';
            c.style.pointerEvents = 'none';
            console.log('[Panel] arka — translateX(-500px), opacity 0, pointer-events none');
        }
    },

    /// AnlaticiPaneliArkayaAt ile arkaya alınan paneli normal pozisyon + opaklığa geri döndürür.
    AnlaticiPaneliOneAl: function() {
        var c = document.getElementById('anlaticiPanelContainer');
        if (c) {
            c.style.transform = 'none';
            c.style.opacity = '1';
            c.style.pointerEvents = 'auto';
            console.log('[Panel] ön — transform none, opacity 1, pointer-events auto');
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
    },

    // ========== BONUS BİTİŞ POPUP (modern DOM, alkış sesi C# tarafında çalar) ==========
    // Parametre: int tutar (TL) — bonus oyunda kazanılan toplam.
    // Kullanıcı TAMAM tıklayınca SendMessage('AnlaticiSeritKopru', 'BonusBitisOnayla') ile
    // Unity coroutine'i devam etsin diye sinyal verir.
    BonusBitisPopupAc: function(tutar) {
        var existing = document.getElementById('bonusBitisPopup');
        if (existing) existing.remove();

        // Tek seferlik <style> ekle (keyframes + hover)
        if (!document.getElementById('bonusBitisStyle')) {
            var style = document.createElement('style');
            style.id = 'bonusBitisStyle';
            style.textContent =
                '@keyframes bonusBitisAcilis {' +
                '  0% { transform: translate(-50%, -50%) scale(0); }' +
                '  60% { transform: translate(-50%, -50%) scale(1.1); }' +
                '  100% { transform: translate(-50%, -50%) scale(1.0); }' +
                '}' +
                '@keyframes bonusBitisParlamaPulse {' +
                '  0%, 100% { opacity: 0.6; }' +
                '  50% { opacity: 1.0; }' +
                '}' +
                '@keyframes bonusBitisKapanis {' +
                '  0% { transform: translate(-50%, -50%) scale(1); opacity: 1; }' +
                '  100% { transform: translate(-50%, -50%) scale(0.95); opacity: 0; }' +
                '}' +
                '#bonusBitisPopup .tamam-btn:hover {' +
                '  transform: scale(1.05);' +
                '  box-shadow: 0 6px 20px rgba(255, 215, 0, 0.8);' +
                '}' +
                '#bonusBitisPopup .parlama-bg {' +
                '  position: absolute; top: 0; left: 0; right: 0; bottom: 0;' +
                '  border-radius: 20px;' +
                '  background: radial-gradient(circle at 50% 50%, rgba(255,215,0,0.18), transparent 70%);' +
                '  animation: bonusBitisParlamaPulse 2s ease-in-out infinite;' +
                '  pointer-events: none;' +
                '}';
            document.head.appendChild(style);
        }

        var popup = document.createElement('div');
        popup.id = 'bonusBitisPopup';
        popup.style.cssText = 'position:fixed;top:50%;left:50%;transform:translate(-50%,-50%) scale(1);width:480px;padding:32px 40px;background:linear-gradient(135deg,#2d1810 0%,#4a2818 50%,#2d1810 100%);border:3px solid #FFD700;border-radius:20px;box-shadow:0 0 60px rgba(255,215,0,0.6),0 8px 32px rgba(0,0,0,0.8);z-index:9999;text-align:center;font-family:inherit;color:#FFFFFF;animation:bonusBitisAcilis 0.6s cubic-bezier(0.34,1.56,0.64,1);';

        var parlama = document.createElement('div');
        parlama.className = 'parlama-bg';
        popup.appendChild(parlama);

        var tebrikler = document.createElement('h1');
        tebrikler.className = 'tebrikler';
        tebrikler.textContent = '🎉 TEBRİKLER 🎉';
        tebrikler.style.cssText = 'font-size:32px;font-weight:900;color:#FFD700;letter-spacing:2px;text-shadow:0 0 20px rgba(255,215,0,0.8);margin:0 0 20px 0;position:relative;';
        popup.appendChild(tebrikler);

        var tutarText = document.createElement('div');
        tutarText.className = 'tutar';
        tutarText.textContent = tutar.toLocaleString('tr-TR') + ' TL';
        tutarText.style.cssText = 'font-size:56px;font-weight:900;color:#4ADE80;text-shadow:0 0 30px rgba(74,222,128,0.8);margin:16px 0;position:relative;';
        popup.appendChild(tutarText);

        var aciklama = document.createElement('p');
        aciklama.className = 'aciklama';
        aciklama.textContent = 'Kazandınız!';
        aciklama.style.cssText = 'font-size:28px;font-weight:700;color:#FFD700;text-shadow:0 0 15px rgba(255,215,0,0.5);letter-spacing:1px;margin:0 0 32px 0;position:relative;';
        popup.appendChild(aciklama);

        var btn = document.createElement('button');
        btn.className = 'tamam-btn';
        btn.textContent = 'TAMAM';
        btn.style.cssText = 'padding:14px 48px;font-size:18px;font-weight:700;background:linear-gradient(135deg,#FFD700,#FFA500);color:#2d1810;border:none;border-radius:10px;cursor:pointer;box-shadow:0 4px 12px rgba(255,215,0,0.5);transition:transform 0.15s,box-shadow 0.15s;font-family:inherit;position:relative;';
        btn.onclick = function() {
            popup.style.animation = 'bonusBitisKapanis 0.2s ease-out forwards';
            setTimeout(function() { if (popup.parentNode) popup.remove(); }, 220);
            // Unity coroutine'ine onay
            try {
                if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
                    unityInstance.SendMessage('AnlaticiSeritKopru', 'BonusBitisOnayla', '');
                }
            } catch(e) {
                console.warn('[BonusBitisPopup] SendMessage hata:', e);
            }
        };
        popup.appendChild(btn);

        document.body.appendChild(popup);
    },

    BonusBitisPopupKapat: function() {
        var p = document.getElementById('bonusBitisPopup');
        if (p) p.remove();
    },

    // ═══ A5 Cazip Popup + Tebrikler Popup — Sahte kutlama havai fişek (DOM Canvas + partikül) ═══
    HavaiFisekBaslat: function() {
        // PAKET 10-FIX: Önceki canvas (A5 bonus tuzağından kalan, partiküller henüz bitmemiş) DOM'da
        // olabilir → early return tebrikler popup için havai fişeği gizlerdi. Eski varsa KALDIR + yeniden başlat.
        var existing = document.getElementById('havaiFisekCanvas');
        if (existing) { existing.remove(); console.log('[HavaiFisek] Eski canvas silindi, yeniden başlatılıyor'); }

        var canvas = document.createElement('canvas');
        canvas.id = 'havaiFisekCanvas';
        canvas.style.cssText = 'position:fixed;top:0;left:0;width:100vw;height:100vh;pointer-events:none;z-index:10000;';
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        document.body.appendChild(canvas);

        var ctx = canvas.getContext('2d');
        var fireworks = [];   // aktif roketler
        var particles = [];   // patlama partikülleri
        var fireworkTimer = 0;
        canvas._aktif = true; // false → yeni roket üretme; tüm partiküller bitince canvas DOM'dan kalkar

        var renkler = [
            [255, 215, 0],    // altın
            [239, 68, 68],    // kırmızı
            [74, 222, 128],   // yeşil
            [251, 146, 60],   // turuncu
            [102, 166, 255],  // mavi
            [255, 128, 204],  // pembe
            [255, 255, 100],  // sarı
            [180, 130, 255]   // mor
        ];

        function rastgeleRenk() {
            return renkler[Math.floor(Math.random() * renkler.length)];
        }

        function rocketYarat() {
            // Popup ekran merkezinde — havai fişeği X aralığını ekran ortasının %50'sine sınırla
            // (çeyreklerin tamamından doğru → popup üzerinde patlama hissi).
            var ekranOrtaX = canvas.width / 2;
            var aralikYari = canvas.width * 0.25;
            var startX = ekranOrtaX - aralikYari + Math.random() * (aralikYari * 2);
            var startY = canvas.height;
            // Hedef Y: ekran üst %15-40 arası (popup başlık üstüne yakın patlasın, alt buton serbest).
            var targetY = canvas.height * 0.15 + Math.random() * canvas.height * 0.25;
            var hiz = 8 + Math.random() * 4;
            fireworks.push({
                x: startX, y: startY,
                targetY: targetY,
                vy: -hiz,
                renk: rastgeleRenk(),
                iz: []
            });
        }

        function patlat(x, y, renk) {
            var sayi = 20 + Math.floor(Math.random() * 10);
            for (var i = 0; i < sayi; i++) {
                var aci = (Math.PI * 2 * i) / sayi;
                var hiz = 2 + Math.random() * 4;
                particles.push({
                    x: x, y: y,
                    vx: Math.cos(aci) * hiz,
                    vy: Math.sin(aci) * hiz,
                    yas: 0,
                    maxYas: 60 + Math.random() * 40,
                    renk: renk,
                    boyut: 2 + Math.random() * 2
                });
            }
        }

        function animate() {
            if (!canvas._aktif && fireworks.length === 0 && particles.length === 0) {
                if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
                return;
            }

            // Karartma yok — pedagojik popup arka planda net görünmeli (fade trail kaldırıldı).
            // Her frame canvas'ı tamamen temizle.
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // Yeni roket gönder (~18 frame'de bir = ~300ms, daha seyrek; %20 olasılıkla 2 roket).
            if (canvas._aktif) {
                fireworkTimer++;
                if (fireworkTimer > 18) {
                    fireworkTimer = 0;
                    rocketYarat();
                    if (Math.random() < 0.2) rocketYarat();
                }
            }

            // Roketler
            for (var i = fireworks.length - 1; i >= 0; i--) {
                var f = fireworks[i];
                f.iz.push({ x: f.x, y: f.y });
                if (f.iz.length > 8) f.iz.shift();
                f.y += f.vy;
                f.vy += 0.05; // hafif yerçekimi (yavaşla)

                // Roket izi (fade)
                for (var j = 0; j < f.iz.length; j++) {
                    var alpha = j / f.iz.length;
                    ctx.fillStyle = 'rgba(' + f.renk[0] + ',' + f.renk[1] + ',' + f.renk[2] + ',' + alpha + ')';
                    ctx.fillRect(f.iz[j].x - 1, f.iz[j].y - 1, 3, 3);
                }

                if (f.y <= f.targetY) {
                    patlat(f.x, f.y, f.renk);
                    fireworks.splice(i, 1);
                }
            }

            // Partiküller
            for (var k = particles.length - 1; k >= 0; k--) {
                var p = particles[k];
                p.x += p.vx;
                p.y += p.vy;
                p.vy += 0.08; // yerçekimi
                p.vx *= 0.99;
                p.vy *= 0.99;
                p.yas++;

                if (p.yas > p.maxYas) {
                    particles.splice(k, 1);
                    continue;
                }

                var pAlpha = 1 - (p.yas / p.maxYas);
                ctx.fillStyle = 'rgba(' + p.renk[0] + ',' + p.renk[1] + ',' + p.renk[2] + ',' + pAlpha + ')';
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.boyut, 0, Math.PI * 2);
                ctx.fill();

                // Glow halkası
                ctx.fillStyle = 'rgba(' + p.renk[0] + ',' + p.renk[1] + ',' + p.renk[2] + ',' + (pAlpha * 0.3) + ')';
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.boyut * 2, 0, Math.PI * 2);
                ctx.fill();
            }

            requestAnimationFrame(animate);
        }

        // Başlangıç burst — 1 roket (sade giriş, popup'ı boğmadan).
        rocketYarat();

        animate();

        // 3 sn sonra otomatik dur — A5 cazip popup'taki BONUS AL butonuna tıklanabilirlik için.
        // Mevcut roketler/partiküller animasyonlarını tamamlayıp canvas DOM'dan otomatik kalkar (~4.5sn toplam).
        // pointer-events:none zaten var (line 385); double-safety: 3sn sonra DOM'dan ayrılma süreci başlar.
        setTimeout(function() {
            if (canvas) {
                canvas._aktif = false;
                console.log('[HavaiFisek] 3sn doldu — otomatik durdu, partiküller bitince canvas temizlenir');
            }
        }, 3000);

        console.log('[HavaiFisek] Başlatıldı (3sn süreli)');
    },

    HavaiFisekDurdur: function() {
        var canvas = document.getElementById('havaiFisekCanvas');
        if (canvas) {
            canvas._aktif = false;
            console.log('[HavaiFisek] Durduruldu — son roketler bitince temizlenecek');
        }
    }

});
