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
  // Mobilde render 30 FPS ile sınırlı: sürekli 60fps WebGL çizimi iOS'te termal/bellek
  // birikimiyle ~30sn sonra sekmeyi çökertiyordu. 30fps yeterince akıcı + yarı yük.
  if (mobil) uygulama.ticker.maxFPS = 30;
  kok.appendChild(uygulama.canvas);
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

/* OYUN_ZOOM: zoom Pixi STAGE içinde uygulanır (canvas fiziksel boyutu 1920x1080 SABİT kalır
   → iOS GPU framebuffer büyümez, bellek patlaması yok). 2 parmak = yakınlaştır/kaydır,
   çift dokunuş = 2x / sıfırla. */
let _zoomK = 1, _panX = 0, _panY = 0;
function zoomUygula() {
  if (!uygulama) return;
  const st = uygulama.stage;
  st.scale.set(_zoomK);
  st.pivot.set(GENISLIK / 2 - _panX, YUKSEKLIK / 2 - _panY); // ekran ortasından zoom + pan
  st.position.set(GENISLIK / 2, YUKSEKLIK / 2);
}
function oyunZoomKur() {
  if (!mobilMi()) return;
  let p = null;
  document.addEventListener("touchstart", (e) => {
    if (e.touches.length === 2) {
      const [a, b] = e.touches;
      p = {
        d0: Math.hypot(a.clientX - b.clientX, a.clientY - b.clientY) || 1,
        k0: _zoomK,
        mx: (a.clientX + b.clientX) / 2, my: (a.clientY + b.clientY) / 2,
        x0: _panX, y0: _panY,
      };
    }
  }, { passive: false });
  document.addEventListener("touchmove", (e) => {
    if (p && e.touches.length === 2) {
      e.preventDefault();
      const [a, b] = e.touches;
      const d = Math.hypot(a.clientX - b.clientX, a.clientY - b.clientY) || 1;
      const mx = (a.clientX + b.clientX) / 2, my = (a.clientY + b.clientY) / 2;
      const lk = letterboxK() || 1;
      _zoomK = Math.min(3, Math.max(1, p.k0 * d / p.d0));
      _panX = p.x0 + (mx - p.mx) / lk;   // parmak kaydırması → 1920 koordinatına çevrilir
      _panY = p.y0 + (my - p.my) / lk;
      if (_zoomK <= 1.01) { _zoomK = 1; _panX = 0; _panY = 0; }
      zoomUygula();
    }
  }, { passive: false });
  document.addEventListener("touchend", (e) => { if (e.touches.length < 2) p = null; });
  let sonT = 0, sonX = 0, sonY = 0;
  document.addEventListener("touchend", (e) => {
    if (e.touches.length === 0 && e.changedTouches.length === 1) {
      const t = e.changedTouches[0], s = Date.now();
      if (s - sonT < 350 && Math.abs(t.clientX - sonX) < 40 && Math.abs(t.clientY - sonY) < 40) {
        if (_zoomK > 1) { _zoomK = 1; _panX = 0; _panY = 0; }
        else {
          _zoomK = 2;
          const lk = letterboxK() || 1;
          const ofsX = (window.innerWidth - GENISLIK * lk) / 2;
          const ofsY = (window.innerHeight - YUKSEKLIK * lk) / 2;
          _panX = GENISLIK / 2 - (t.clientX - ofsX) / lk;   // çift dokunulan noktaya odaklan
          _panY = YUKSEKLIK / 2 - (t.clientY - ofsY) / lk;
        }
        zoomUygula();
      }
      sonT = s; sonX = t.clientX; sonY = t.clientY;
    }
  });
}
