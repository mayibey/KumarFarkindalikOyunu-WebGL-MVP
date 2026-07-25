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

  // --- SOL: sık grid + tahta SARAR (oyun ekranıyla aynı mantık) ---
  const gMerkez = { x: 610, y: 500 };
  const HUCRE = 132, BOSLUK = 9;
  const gW = 6 * HUCRE + 5 * BOSLUK, gH = 5 * HUCRE + 4 * BOSLUK;
  const IC_W_ORAN = 0.79, IC_H_ORAN = 0.555;
  const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
  tahta.anchor.set(0.5);
  tahta.position.set(gMerkez.x, gMerkez.y);
  tahta.scale.set((gW / IC_W_ORAN) / tahta.texture.width, (gH / IC_H_ORAN) / tahta.texture.height);
  kok.addChild(tahta);

  const demo = new SlotGorunum(uygulama, dokular, {
    x: gMerkez.x - gW / 2, y: gMerkez.y - gH / 2,
    hucre: HUCRE, boslukX: BOSLUK, boslukY: BOSLUK,
  });
  kok.addChild(demo.kok);
  const maske = new PIXI.Graphics()
    .rect(gMerkez.x - gW / 2 - 4, gMerkez.y - gH / 2 - 4, gW + 8, gH + 8).fill(0xffffff);
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
  logo.anchor.set(0.5); logo.position.set(1440, 250);
  logo.scale.set(Math.min(680 / logo.texture.width, 1));
  kok.addChild(logo);

  // --- SAĞ: 2 süslü buton (btn_bos_plaka + altın yazı) ---
  function suslButon(y, satirlar, cb, olcek = 1) {
    const c = new PIXI.Container();
    c.position.set(1440, y);
    const plaka = new PIXI.Sprite(dokular.btn_bos_plaka);
    plaka.anchor.set(0.5);
    plaka.scale.set((620 * olcek) / plaka.texture.width);  // Unity butonları daha geniş
    c.addChild(plaka);
    const t = new PIXI.Text({ text: satirlar, style: {
      fontFamily: "LilitaOne", fontSize: 38 * olcek, fill: 0xffe08a, align: "center",
      stroke: { color: 0x5a1a00, width: 5 }, lineHeight: 44 * olcek } });
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
    suslButon(660, "SENARYOLU\nOYUNA BAŞLA", () => { kapat(); onSenaryo("Misafir"); }, 1.0);
    suslButon(870, "MANİPÜLASYON\nPANELİNE GİT", () => { kapat(); onPanel(); }, 0.85);
  }

  // Güvenli kapat: demo döngüsünü durdur + görünmez yap; devam eden tween bitene kadar
  // destroy'u geciktir (yoksa yok edilmiş sprite'a position atanır → null hatası).
  function kapat() {
    demoCalisiyor = false;
    kok.visible = false;
    setTimeout(() => { try { kok.destroy({ children: true }); } catch (e) {} }, 1200);
  }
  return kok;
}
