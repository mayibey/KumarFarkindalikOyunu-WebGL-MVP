// WebAudio ses sistemi — HizVeSesServisi karşılığı.
// iOS kilidi: ilk kullanıcı dokunuşunda context resume. Pitch 0.96-1.06 (Unity randomizasyonu).
// AudioContext, SUNUM_SES_YAKALA sarmalayıcısından geçer → sunumun 🔊 düğmesi bunu da susturur.
const ctx = new (window.AudioContext || window.webkitAudioContext)();
const master = ctx.createGain();
master.connect(ctx.destination);

const tamponlar = new Map();

export async function sesYukle(ad, yol) {
  // Hata KESİNLİKLE yutulur: tek bir bozuk/desteklenmeyen ses dosyası oyunu düşüremez.
  try {
    const yanit = await fetch(yol);
    const ham = await yanit.arrayBuffer();
    tamponlar.set(ad, await ctx.decodeAudioData(ham));
  } catch (e) {
    console.warn(`[ses] yüklenemedi: ${ad} (${yol})`, e?.message || e);
  }
}

export function sesCal(ad, { ses = 1, pitchRasgele = false, baslangic = 0, sure = null, dongu = false } = {}) {
  const t = tamponlar.get(ad);
  if (!t || ctx.state !== "running") return null;
  const kaynak = ctx.createBufferSource();
  kaynak.buffer = t;
  if (pitchRasgele) kaynak.playbackRate.value = 0.96 + Math.random() * 0.10; // OyunYoneticisi.cs pitch aralığı
  kaynak.loop = dongu;
  const g = ctx.createGain(); g.gain.value = ses;
  kaynak.connect(g); g.connect(master);
  if (sure != null) kaynak.start(0, baslangic, sure); else kaynak.start(0, baslangic);
  return kaynak;
}

export function anaSes(v) { master.gain.value = v; } // AudioListener.volume karşılığı (SunumSesAyarla)

export function sesKilidiKur() {
  const ac = () => { if (ctx.state !== "running") ctx.resume(); };
  ["pointerdown", "touchstart", "keydown"].forEach((t) =>
    document.addEventListener(t, ac, { once: false, passive: true }));
}
