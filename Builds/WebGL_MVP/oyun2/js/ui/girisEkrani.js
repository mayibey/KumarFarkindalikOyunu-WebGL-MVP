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

  // --- SOL: grid + tahta SARAR. Unity 01_GirisScene SlotGrid: anchoredPos(-383,13)
  //     → merkez (577, 527). Hücre mantığı 02 ile aynı (165x120 → sütun 170, satır 125). ---
  const gMerkez = { x: 577, y: 527 };
  const HUCRE = 120, BOSLUK_X = 50, BOSLUK_Y = 5;
  const gW = 6 * HUCRE + 5 * BOSLUK_X, gH = 5 * HUCRE + 4 * BOSLUK_Y;
  const IC_W_ORAN = 0.81, IC_H_ORAN = 0.74;
  const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
  tahta.anchor.set(0.5);
  tahta.position.set(gMerkez.x, gMerkez.y);
  tahta.scale.set((gW / IC_W_ORAN) / tahta.texture.width, (gH / IC_H_ORAN) / tahta.texture.height);
  kok.addChild(tahta);

  const demo = new SlotGorunum(uygulama, dokular, {
    x: gMerkez.x - gW / 2, y: gMerkez.y - gH / 2,
    hucre: HUCRE, boslukX: BOSLUK_X, boslukY: BOSLUK_Y,
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
  // Unity 01 Logo: anchoredPos(610.49,187) → merkez (1570,353), sizeDelta genişlik 697.
  const logo = new PIXI.Sprite(dokular.logo_kumar_yazisi);
  logo.anchor.set(0.5); logo.position.set(1570, 353);
  logo.scale.set(697 / logo.texture.width);
  kok.addChild(logo);

  // --- SAĞ: 2 süslü buton (btn_bos_plaka + altın yazı) ---
  // Unity 01: SenaryoluOyunButton merkez (1533,685), ManipulasyonPaneliButton (1533,955), boyut 480x238.
  function suslButon(y, satirlar, cb, olcek = 1) {
    const c = new PIXI.Container();
    c.position.set(1533, y);
    const plaka = new PIXI.Sprite(dokular.btn_bos_plaka);
    plaka.anchor.set(0.5);
    plaka.scale.set((480 * olcek) / plaka.texture.width);   // Unity sizeDelta genişlik 480
    c.addChild(plaka);
    const t = new PIXI.Text({ text: satirlar, style: {
      fontFamily: "LilitaOne, TitanOne, sans-serif", fontSize: 36 * olcek, fill: 0xffe08a, align: "center",
      stroke: { color: 0x5a1a00, width: 5 }, lineHeight: 40 * olcek } });
    t.anchor.set(0.5); c.addChild(t);
    c.eventMode = "static"; c.cursor = "pointer";
    c.on("pointerover", () => c.scale.set(1.05));
    c.on("pointerout", () => c.scale.set(1));
    c.on("pointertap", cb);
    kok.addChild(c);
    return c;
  }

  // Unity 01: 2 buton merkez_y 685 ve 955 (arası 270). Kayıt varsa üstüne DEVAM (3 buton sığdır).
  const kaydetVar = kayitVar();
  if (kaydetVar) {
    const k = yukle();
    suslButon(505, "DEVAM ET", () => { kapat(); onDevam(k); }, 0.92);
    suslButon(730, "SENARYOLU\nOYUNA BAŞLA", () => { sil(); kapat(); onSenaryo("Misafir"); }, 0.92);
    suslButon(955, "MANİPÜLASYON\nPANELİNE GİT", () => { kapat(); onPanel(); }, 0.92);
  } else {
    suslButon(685, "SENARYOLU\nOYUNA BAŞLA", () => { kapat(); onSenaryo("Misafir"); }, 1.0);
    suslButon(955, "MANİPÜLASYON\nPANELİNE GİT", () => { kapat(); onPanel(); }, 1.0);
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
