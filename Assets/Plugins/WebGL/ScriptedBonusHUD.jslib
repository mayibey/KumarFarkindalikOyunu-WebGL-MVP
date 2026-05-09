// Scripted Bonus HUD — sağ üst köşede küçük HTML overlay (Sweet Bonanza tarzı).
// AnlaticiSeritKopru gibi iframe yerine doğrudan DOM div manipülasyonu (HUD küçük olduğu için).
mergeInto(LibraryManager.library, {

    BonusHUDGoster: function() {
        // Mevcut HUD'u kaldır (idempotent)
        var existing = document.getElementById('scriptedBonusHud');
        if (existing) existing.remove();

        var hud = document.createElement('div');
        hud.id = 'scriptedBonusHud';
        hud.style.cssText =
            'position:fixed;top:20px;right:20px;width:280px;padding:16px;' +
            'background:rgba(26,26,46,0.85);border:2px solid #ffd700;border-radius:12px;' +
            'font-family:Arial,sans-serif;color:white;' +
            'box-shadow:0 0 20px rgba(255,215,0,0.4);z-index:9999;' +
            'animation:scriptedBonusHudSlideIn 0.4s ease-out;';

        // CSS keyframes (eğer henüz tanımlı değilse)
        if (!document.getElementById('scriptedBonusHudStyles')) {
            var style = document.createElement('style');
            style.id = 'scriptedBonusHudStyles';
            style.textContent =
                '@keyframes scriptedBonusHudSlideIn {' +
                '  from { transform: translateX(120%); opacity: 0; }' +
                '  to { transform: translateX(0); opacity: 1; }' +
                '}' +
                '@keyframes scriptedBonusHudKazancParla {' +
                '  0%,100% { color: #ffd700; }' +
                '  50% { color: #fff8a0; text-shadow: 0 0 8px rgba(255,215,0,0.8); }' +
                '}' +
                '#scriptedBonusHud .kazancParla {' +
                '  animation: scriptedBonusHudKazancParla 0.6s ease-out;' +
                '}';
            document.head.appendChild(style);
        }

        hud.innerHTML =
            '<div style="font-size:16px;margin-bottom:8px;">' +
            '  🎰 KALAN SPİN: <span id="scriptedBonusHudSpin">10</span> / 10' +
            '</div>' +
            '<div style="font-size:14px;color:#ffd700;">' +
            '  💰 OTURUM KAZANCI: <span id="scriptedBonusHudKazanc">0</span> TL' +
            '</div>';
        document.body.appendChild(hud);
    },

    BonusHUDGizle: function() {
        var hud = document.getElementById('scriptedBonusHud');
        if (hud) {
            hud.style.transition = 'transform 0.3s ease-in, opacity 0.3s ease-in';
            hud.style.transform = 'translateX(120%)';
            hud.style.opacity = '0';
            setTimeout(function() { if (hud.parentNode) hud.parentNode.removeChild(hud); }, 350);
        }
    },

    BonusHUDGuncelle: function(spin, kazanc) {
        var spinEl = document.getElementById('scriptedBonusHudSpin');
        var kazancEl = document.getElementById('scriptedBonusHudKazanc');
        if (spinEl) spinEl.textContent = spin;
        if (kazancEl) {
            // Format TL: 1234 → "1.234"
            var formatted = kazanc.toString();
            if (kazanc >= 1000) {
                formatted = kazanc.toLocaleString('tr-TR');
            }
            // Eğer değer değiştiyse parlama animasyonu tetikle
            if (kazancEl.textContent !== formatted) {
                kazancEl.textContent = formatted;
                kazancEl.classList.remove('kazancParla');
                // Reflow ile re-trigger
                void kazancEl.offsetWidth;
                kazancEl.classList.add('kazancParla');
            }
        }
    }

});
