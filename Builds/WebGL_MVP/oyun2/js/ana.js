// Giriş noktası — F1: sahne + shim + canlı doğrulama animasyonu (6x5 dönen ızgara).
// Sonraki fazlarda burası sahne yönlendiricisine dönüşür (giris / senaryolu / admin).
import { sahneKur, uygulama, GENISLIK, YUKSEKLIK } from "./cekirdek/sahne.js";
import { shimKur, kopruKaydet } from "./kopru/sendMessageShim.js";

shimKur();

// F1 geçici köprü kayıtları: sunum butonları shim'e çarptığında log görelim (karşılıksız değil).
kopruKaydet("SunumKoprusu", {
  SunumAsamaGit: (n) => console.log("[F1] SunumAsamaGit", n, "— senaryo motoru F5'te geliyor"),
  SunumPanelGit: () => console.log("[F1] SunumPanelGit — admin sahnesi F6'da geliyor"),
  SunumSesAyarla: (a) => console.log("[F1] SunumSesAyarla", a),
});

await sahneKur();

// --- F1 doğrulama sahnesi: 6x5 ızgarada akan sembol yer tutucuları + başlık ---
const S = new PIXI.Container();
uygulama.stage.addChild(S);

const baslik = new PIXI.Text({
  text: "KUMAR KAZANDIRMAZ — PixiJS /oyun2 (Faz 1 iskelet)",
  style: { fill: 0xd4a24a, fontSize: 42, fontWeight: "bold", fontFamily: "Arial" },
});
baslik.anchor.set(0.5);
baslik.position.set(GENISLIK / 2, 90);
S.addChild(baslik);

const RENKLER = [0xe63946, 0x4ade80, 0x60a5fa, 0xfb923c, 0xa78bfa, 0xf4d678, 0x34d399, 0xf87171];
const SUTUN = 6, SATIR = 5, HUCRE = 150, BOSLUK = 14;
const izgara = new PIXI.Container();
const gw = SUTUN * HUCRE + (SUTUN - 1) * BOSLUK, gh = SATIR * HUCRE + (SATIR - 1) * BOSLUK;
izgara.position.set((GENISLIK - gw) / 2, (YUKSEKLIK - gh) / 2 + 40);
S.addChild(izgara);

const hucreler = [];
for (let y = 0; y < SATIR; y++) for (let x = 0; x < SUTUN; x++) {
  const g = new PIXI.Graphics()
    .roundRect(0, 0, HUCRE, HUCRE, 22)
    .fill(RENKLER[(x + y * SUTUN) % RENKLER.length]);
  g.pivot.set(HUCRE / 2, HUCRE / 2);
  g.position.set(x * (HUCRE + BOSLUK) + HUCRE / 2, y * (HUCRE + BOSLUK) + HUCRE / 2);
  izgara.addChild(g);
  hucreler.push({ g, faz: (x * 0.6 + y * 0.9) });
}

let fpsMetin = new PIXI.Text({ text: "", style: { fill: 0x888888, fontSize: 24, fontFamily: "monospace" } });
fpsMetin.position.set(24, YUKSEKLIK - 48);
S.addChild(fpsMetin);

let t = 0;
uygulama.ticker.add((tk) => {
  t += tk.deltaMS / 1000;
  for (const h of hucreler) {
    const s = 1 + Math.sin(t * 2 + h.faz) * 0.06;
    h.g.scale.set(s);
    h.g.rotation = Math.sin(t + h.faz) * 0.05;
  }
  fpsMetin.text = `${Math.round(tk.FPS)} fps · Pixi ${PIXI.VERSION}`;
});

console.log("[F1] sahne hazır — Pixi", PIXI.VERSION);
