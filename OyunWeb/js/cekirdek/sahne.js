// Sahne çekirdeği: 1920x1080 mantıksal sahne + pencereye oranlı letterbox + OYUN_ZOOM.
// Unity'deki OLCEK_WRAPPER + Canvas(1920x1080) ikilisinin tek dosyalık karşılığı.
export const GENISLIK = 1920;
export const YUKSEKLIK = 1080;

export let uygulama = null; // PIXI.Application

export async function sahneKur() {
  const kok = document.getElementById("sahne");
  const mobil = mobilMi();
  uygulama = new PIXI.Application();
  await uygulama.init({
    width: GENISLIK,
    height: YUKSEKLIK,
    background: 0x0b0a09,
    antialias: !mobil,                 // mobilde AA kapalı: WebGL bellek/GPU yükü azalır
    powerPreference: mobil ? "low-power" : "high-performance",
    resolution: mobil ? 1 : Math.min(window.devicePixelRatio || 1, 2),
    autoDensity: true,
  });
  // Mobilde render 30 FPS ile sınırlı: sürekli 60fps WebGL çizimi iOS'te termal/bellek yükü.
  if (mobil) uygulama.ticker.maxFPS = 30;
  // RENDER-ON-DEMAND (mobil): ekranda animasyon/dokunma yokken ticker DURUR (GPU dinlenir →
  // iOS'te uzun sürede biriken termal/bellek çökmesi önlenir). Dokunma/tıklama/animasyon uyandırır.
  if (mobil) {
    let uykuT = null;
    window.__uyandir = () => {
      if (uygulama && !uygulama.ticker.started) uygulama.ticker.start();
      if (uykuT) clearTimeout(uykuT);
      uykuT = setTimeout(() => { if (uygulama) uygulama.ticker.stop(); }, 3500);
    };
    ["pointerdown", "pointermove", "touchstart", "touchmove", "keydown", "wheel"].forEach((ev) =>
      document.addEventListener(ev, () => window.__uyandir(), { passive: true }));
    window.__uyandir();
  }
  kok.appendChild(uygulama.canvas);
  // DOM overlay katmanı: anlatıcı/panel iframe'leri + modallar buraya eklenir. ZOOM yalnız
  // buna uygulanır (oyun canvas'ı zoom'dan muaf → kullanıcı isteği: yazılı panelleri büyüt, oyunu değil).
  const domUst = document.createElement("div");
  domUst.id = "domUst";
  Object.assign(domUst.style, {
    position: "absolute", left: "0", top: "0", width: GENISLIK + "px", height: YUKSEKLIK + "px",
    pointerEvents: "none", transformOrigin: "center center",
  });
  kok.appendChild(domUst);
  const y = document.getElementById("yukleniyor");
  if (y) y.remove();

  olcekle();
  window.addEventListener("resize", olcekle);
  window.addEventListener("orientationchange", olcekle);
  oyunZoomKur();
  return uygulama;
}

export function mobilMi() {
  return /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);
}

// Pencereye oranlı sığdır (letterbox). Sunum iframe'i tam 1920x1080 verir → ölçek 1.
// ÖNEMLİ: CSS transform SADECE letterbox yapar; zoom canvas'ı CSS ile büyütmez (iOS'te
// CSS-scaled WebGL canvas dev bir GPU compose katmanı ayırıp sekmeyi ANINDA çökertiyordu).
function letterboxK() {
  return Math.min(window.innerWidth / GENISLIK, window.innerHeight / YUKSEKLIK);
}
function olcekle() {
  const s = document.getElementById("sahne");
  s.style.transform = `translate(-50%,-50%) scale(${letterboxK()})`;
  s.style.left = "50%";
  s.style.top = "50%";
}

/* OYUN_ZOOM: zoom yalnız DOM overlay katmanına (#domUst: yazılı paneller/modallar) uygulanır.
   Oyun canvas'ı SABİT kalır (kullanıcı: oyunu değil panelleri yakınlaştır). CSS scale yalnız
   DOM'a — canvas'a değil — bu yüzden iOS'te bellek patlaması yok. 2 parmak zoom/kaydır, çift dokun 2x. */
let _zoomK = 1, _originX = GENISLIK / 2, _originY = YUKSEKLIK / 2;
function zoomUygula() {
  const d = document.getElementById("domUst");
  if (!d) return;
  // ZOOM-TO-POINT: parmakların ortasından sabit yakınlaşır (pan yok → yakınlaştırırken KAYMAZ).
  d.style.transformOrigin = `${_originX}px ${_originY}px`;
  d.style.transform = _zoomK <= 1.001 ? "" : `scale(${_zoomK})`;
}
// ekran pikseli → domUst (1920x1080) koordinatı
function ekranToDom(cx, cy) {
  const lk = letterboxK() || 1;
  const ofsX = (window.innerWidth - GENISLIK * lk) / 2;
  const ofsY = (window.innerHeight - YUKSEKLIK * lk) / 2;
  return [(cx - ofsX) / lk, (cy - ofsY) / lk];
}
function oyunZoomKur() {
  if (!mobilMi()) return;
  let p = null;
  document.addEventListener("touchstart", (e) => {
    if (e.touches.length === 2) {
      const [a, b] = e.touches;
      const mx = (a.clientX + b.clientX) / 2, my = (a.clientY + b.clientY) / 2;
      [_originX, _originY] = ekranToDom(mx, my);        // zoom odağı = parmakların ortası (sabit)
      p = { d0: Math.hypot(a.clientX - b.clientX, a.clientY - b.clientY) || 1, k0: _zoomK };
      zoomUygula();
    }
  }, { passive: false });
  document.addEventListener("touchmove", (e) => {
    if (p && e.touches.length === 2) {
      e.preventDefault();
      const [a, b] = e.touches;
      const d = Math.hypot(a.clientX - b.clientX, a.clientY - b.clientY) || 1;
      _zoomK = Math.min(3, Math.max(1, p.k0 * d / p.d0));   // yalnız ölçek değişir → kayma yok
      zoomUygula();
    }
  }, { passive: false });
  document.addEventListener("touchend", (e) => { if (e.touches.length < 2) p = null; });
  let sonT = 0, sonX = 0, sonY = 0;
  document.addEventListener("touchend", (e) => {
    if (e.touches.length === 0 && e.changedTouches.length === 1) {
      const t = e.changedTouches[0], s = Date.now();
      if (s - sonT < 350 && Math.abs(t.clientX - sonX) < 40 && Math.abs(t.clientY - sonY) < 40) {
        if (_zoomK > 1) { _zoomK = 1; }
        else { _zoomK = 2; [_originX, _originY] = ekranToDom(t.clientX, t.clientY); }  // dokunulan noktaya odaklan
        zoomUygula();
      }
      sonT = s; sonX = t.clientX; sonY = t.clientY;
    }
  });
}
