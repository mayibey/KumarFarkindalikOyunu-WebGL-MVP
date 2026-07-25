// Unity coroutine desenlerinin karşılığı: bekle (WaitForSeconds), bekleKosul (WaitUntil).
export const bekle = (sn) => new Promise((r) => setTimeout(r, sn * 1000));
export const bekleKosul = (fn, aralikMs = 50) => new Promise((r) => {
  const t = setInterval(() => { if (fn()) { clearInterval(t); r(); } }, aralikMs);
});
// Basit tween: ticker tabanlı, easing fonksiyonlu
export function tween(uygulama, { sure, guncelle, easing = (t) => t }) {
  return new Promise((resolve) => {
    let g = 0;
    const adim = (tk) => {
      if (window.__uyandir) window.__uyandir();   // animasyon boyunca render uyanık kalsın (render-on-demand)
      g += tk.deltaMS / 1000;
      const t = Math.min(1, g / sure);
      guncelle(easing(t), t);
      if (t >= 1) { uygulama.ticker.remove(adim); resolve(); }
    };
    if (window.__uyandir) window.__uyandir();      // uykudaysa ticker'ı başlat
    uygulama.ticker.add(adim);
  });
}
export const easeOutCubic = (t) => 1 - Math.pow(1 - t, 3);
export const smoothStep = (t) => t * t * (3 - 2 * t);
