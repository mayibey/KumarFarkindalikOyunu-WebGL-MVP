// Giriş ekranı — 01_GirisScene BİREBİR: solda altın çerçeveli tahtada CANLI slot demo,
// sağda KUMAR KAZANDIRMAZ logosu + 2 süslü altın buton (btn_bos_plaka + yazı).
// GirisDemoAnimator karşılığı: sol tahtada sürekli tumble demo döner.
import { SEMBOLLER } from "../../veri/sabitler.js";
import { rngYap } from "../motor/rng.js";
import { izgaraDoldur } from "../motor/doldurucu.js";
import { spinUret } from "../motor/spinMotoru.js";
import { SlotGorunum } from "./slotGorunum.js";
import { kayitVar, yukle, sil } from "../cekirdek/kayit.js";

export function girisEkraniKur(uygulama, dokular, { onSenaryo, onDevam, onPanel }) {
  const kok = new PIXI.Container();

  const arka = new PIXI.Sprite(dokular.arkaplan_oyun);
  arka.width = 1920; arka.height = 1080; kok.addChild(arka);

  // --- SOL: altın çerçeveli tahta + canlı slot demo ---
  const TAHTA_H = 760;
  const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
  tahta.anchor.set(0.5);
  tahta.position.set(600, 500);
  tahta.scale.set(TAHTA_H / tahta.texture.height);
  kok.addChild(tahta);

  const tahtaW = tahta.texture.width * tahta.scale.x;
  const icX0 = tahta.x - tahtaW / 2 + 0.105 * tahtaW, icX1 = tahta.x - tahtaW / 2 + 0.895 * tahtaW;
  const icY0 = tahta.y - TAHTA_H / 2 + 0.220 * TAHTA_H, icY1 = tahta.y - TAHTA_H / 2 + 0.775 * TAHTA_H;
  const icW = icX1 - icX0, icH = icY1 - icY0;
  const HUCRE = Math.min(icW / 6 * 0.92, (icH - 24) / 5);
  const bY = (icH - 5 * HUCRE) / 4, bX = (icW - 6 * HUCRE) / 5;
  const gW = 6 * HUCRE + 5 * bX, gH = 5 * HUCRE + 4 * bY;

  const demo = new SlotGorunum(uygulama, dokular, {
    x: (icX0 + icX1) / 2 - gW / 2, y: (icY0 + icY1) / 2 - gH / 2,
    hucre: HUCRE, boslukX: bX, boslukY: bY,
  });
  kok.addChild(demo.kok);
  const maske = new PIXI.Graphics().rect(icX0, icY0, icW, icH).fill(0xffffff);
  kok.addChild(maske); demo.kok.mask = maske;

  const drng = rngYap(20260725);
  demo.gridGoster(izgaraDoldur(drng, {}));
  let demoCalisiyor = true;
  async function demoDongu() {
    while (demoCalisiyor) {
      const s = spinUret(1000, { egilimYuzde: 90, minKat: 0, maksKat: 0, aktifSenaryo: "normal", zorluk: 4, maxReroll: 100 }, drng);
      demo.gridGoster(s.baslangicGrid);
      for (const ad of s.adimlar) { if (!demoCalisiyor) return; await demo.tumbleAdimiOynat(ad); }
      await new Promise((r) => setTimeout(r, 1400));
    }
  }
  demoDongu();

  // --- SAĞ: logo ---
  const logo = new PIXI.Sprite(dokular.logo_kumar_yazisi);
  logo.anchor.set(0.5); logo.position.set(1420, 300);
  logo.scale.set(Math.min(720 / logo.texture.width, 1));
  kok.addChild(logo);

  // --- SAĞ: 2 süslü buton (btn_bos_plaka + altın yazı) ---
  function suslButon(y, satirlar, cb, olcek = 1) {
    const c = new PIXI.Container();
    c.position.set(1420, y);
    const plaka = new PIXI.Sprite(dokular.btn_bos_plaka);
    plaka.anchor.set(0.5);
    plaka.scale.set((460 * olcek) / plaka.texture.width);
    c.addChild(plaka);
    const t = new PIXI.Text({ text: satirlar, style: {
      fontFamily: "LilitaOne", fontSize: 34 * olcek, fill: 0xffe08a, align: "center",
      stroke: { color: 0x5a1a00, width: 5 }, lineHeight: 40 * olcek } });
    t.anchor.set(0.5); c.addChild(t);
    c.eventMode = "static"; c.cursor = "pointer";
    c.on("pointerover", () => c.scale.set(1.05));
    c.on("pointerout", () => c.scale.set(1));
    c.on("pointertap", cb);
    kok.addChild(c);
    return c;
  }

  const kaydetVar = kayitVar();
  if (kaydetVar) {
    const k = yukle();
    suslButon(600, "DEVAM ET", () => { kapat(); onDevam(k); }, 0.85);
    suslButon(760, "SENARYOLU\nOYUNA BAŞLA", () => { sil(); kapat(); onSenaryo("Misafir"); }, 1);
    suslButon(910, "MANİPÜLASYON\nPANELİNE GİT", () => { kapat(); onPanel(); }, 0.82);
  } else {
    suslButon(660, "SENARYOLU\nOYUNA BAŞLA", () => { kapat(); onSenaryo("Misafir"); }, 1.05);
    suslButon(850, "MANİPÜLASYON\nPANELİNE GİT", () => { kapat(); onPanel(); }, 0.85);
  }

  function kapat() { demoCalisiyor = false; kok.visible = false; kok.destroy({ children: true }); }
  return kok;
}
