// unityInstance.SendMessage taklidi: sunum/panel/anlatıcı HTML'leri Unity'yle bu API
// üzerinden konuşuyor; TEK SATIR değişmeden çalışmaları için aynı imzayı sunuyoruz.
// Hedef nesne adları Unity GameObject adlarıyla birebir aynı tutulur.
const hedefler = new Map(); // "SunumKoprusu" -> { SunumAsamaGit: fn, ... }

export function kopruKaydet(hedefAdi, metodlar) {
  hedefler.set(hedefAdi, { ...(hedefler.get(hedefAdi) || {}), ...metodlar });
}

export function shimKur() {
  window.unityInstance = {
    SendMessage(hedefAdi, metodAdi, arg) {
      const h = hedefler.get(hedefAdi);
      if (!h || typeof h[metodAdi] !== "function") {
        console.warn(`[shim] karşılıksız SendMessage: ${hedefAdi}.${metodAdi}`, arg);
        return;
      }
      try { h[metodAdi](arg); }
      catch (e) { console.error(`[shim] ${hedefAdi}.${metodAdi} hata:`, e); }
    },
  };
  // Unity sayfasındaki panel köprüsü deseniyle uyum (index.html'deki _panelUnityInst akışı):
  window.addEventListener("message", (e) => {
    const d = e.data;
    if (!d || d.source !== "yoneticiPanel") return;
    window.unityInstance.SendMessage("PanelKopru", "AyarAl",
      JSON.stringify({ source: d.source, key: d.key, value: String(d.value) }));
  });
}
