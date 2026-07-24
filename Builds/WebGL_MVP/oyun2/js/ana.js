// Giriş noktası — F4: Unity 02 sahnesi düzenine göre oynanabilir ekran.
// Yerleşim referansı: Unity ekran görüntüleri (KAZANÇ plakası üst-orta, Bakiye sol-alt,
// Bahis sağ-alt, kırmızı -/+ ve SPIN alt-orta, AYARLAR çarkı sağ-alt).
import { sahneKur, uygulama, GENISLIK, YUKSEKLIK } from "./cekirdek/sahne.js";
import { shimKur, kopruKaydet } from "./kopru/sendMessageShim.js";
import { SEMBOLLER, EKONOMI, MODLAR } from "../veri/sabitler.js";
import { spinUret } from "./motor/spinMotoru.js";
import { izgaraDoldur } from "./motor/doldurucu.js";
import { rngYap } from "./motor/rng.js";
import { SlotGorunum } from "./ui/slotGorunum.js";
import { WinFeedback } from "./ui/winFeedback.js";
import { sesYukle, sesCal, sesKilidiKur, anaSes } from "./cekirdek/ses.js";

shimKur();
kopruKaydet("SunumKoprusu", {
  SunumAsamaGit: (n) => console.log("[F4] SunumAsamaGit", n, "— F5'te bağlanacak"),
  SunumPanelGit: () => console.log("[F4] SunumPanelGit — F6'da bağlanacak"),
  SunumSesAyarla: (a) => anaSes(a === 1 ? 1 : 0),
});
sesKilidiKur();

await sahneKur();

const font = new FontFace("LilitaOne", "url(varlik/font/LilitaOne-Regular.ttf)");
await font.load(); document.fonts.add(font);

const dokuAdlari = [...SEMBOLLER, "arkaplan_oyun", "oyun_tahtasi", "btn_spin",
  "logo_kumar_yazisi", "btn_bahis_artir", "btn_bahis_azalt", "btn_ayarlar",
  "etiket_bakiye", "etiket_bahis", "etiket_kazanc", "btn_bos_plaka"];
const dokular = {};
await Promise.all(dokuAdlari.map(async (ad) => {
  dokular[ad] = await PIXI.Assets.load(`varlik/gorsel/${ad}.webp`);
}));
await Promise.all([
  sesYukle("tumble_pop", "varlik/ses/tumble_pop.mp3"),
  sesYukle("alkis", "varlik/ses/alkis.mp3"),
  sesYukle("sayac_tik", "varlik/ses/sayac_tik.mp3"),
  sesYukle("kayip_horn", "varlik/ses/kayip_horn.mp3"),
  sesYukle("fon", "varlik/ses/fon_muzigi.mp3"),
]);
let fonBasladi = false;
document.addEventListener("pointerdown", () => {
  if (!fonBasladi) { fonBasladi = true; setTimeout(() => sesCal("fon", { ses: 0.25, dongu: true }), 300); }
}, { once: true });

const S = uygulama.stage;

// --- Arka plan + logo ---
const arka = new PIXI.Sprite(dokular.arkaplan_oyun);
arka.width = GENISLIK; arka.height = YUKSEKLIK;
S.addChild(arka);

const logo = new PIXI.Sprite(dokular.logo_kumar_yazisi);
logo.anchor.set(0.5, 0); logo.position.set(255, 18);
logo.scale.set(Math.min(430 / logo.texture.width, 1));
S.addChild(logo);

// --- Oyun tahtası + İÇİNE oturan ızgara (Unity oranı: tahta geniş, meyveler iri) ---
const TAHTA_H = 760;
const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
tahta.anchor.set(0.5);
tahta.position.set(GENISLIK / 2, 505);
tahta.scale.set(TAHTA_H / tahta.texture.height);
S.addChild(tahta);

const tahtaW = tahta.texture.width * tahta.scale.x;
const icW = tahtaW * 0.86, icH = TAHTA_H * 0.64;
const BOSLUK = 6;
const HUCRE = Math.min((icW - 5 * BOSLUK) / 6, (icH - 4 * BOSLUK) / 5);
const gridW = 6 * HUCRE + 5 * BOSLUK, gridH = 5 * HUCRE + 4 * BOSLUK;

const slot = new SlotGorunum(uygulama, dokular, {
  x: tahta.position.x - gridW / 2, y: tahta.position.y - gridH / 2 + 10,
  hucre: HUCRE, bosluk: BOSLUK,
});
S.addChild(slot.kok);

// --- Hoş geldin kutusu (Unity birebir: lacivert zemin, ince altın çerçeve, beyaz yazı) ---
const hosKutu = new PIXI.Graphics()
  .roundRect(-170, -30, 340, 60, 14)
  .fill(0x0e1a38)
  .stroke({ color: 0xd8a63a, width: 3 });
hosKutu.position.set(GENISLIK - 230, 58);
S.addChild(hosKutu);
const hosT = new PIXI.Text({ text: "Hoş Geldiniz Misafir", style: {
  fontFamily: "Georgia, serif", fontSize: 28, fontWeight: "bold", fill: 0xffffff } });
hosT.anchor.set(0.5); hosT.position.set(GENISLIK - 230, 58);
S.addChild(hosT);

// --- Ekonomi durumu ---
const rng = rngYap((Math.floor(performance.now()) ^ 0x5eed) >>> 0);
let bakiye = EKONOMI.baslangicBakiye, bahis = 500, kazancSon = 0, spinAktif = false;

// --- Plakalar (KAZANÇ üst-orta, Bakiye sol-alt, Bahis sağ-alt) ---
function plaka(doku, x, y, hedefW, yaziBoyut, yaziDy = 0) {
  const p = new PIXI.Sprite(doku);
  p.anchor.set(0.5); p.position.set(x, y);
  p.scale.set(hedefW / p.texture.width);
  S.addChild(p);
  const t = new PIXI.Text({ text: "", style: {
    fontFamily: "LilitaOne", fontSize: yaziBoyut, fill: 0xffdf4d,
    stroke: { color: 0x2a1800, width: 5 } } });
  t.anchor.set(0.5); t.position.set(x, y + yaziDy);
  S.addChild(t);
  return t;
}
// Plakalarin içinde "BAKİYE:/BAHİS" etiketi BASILI — biz yalnız DEĞERİ yazarız (sağa kaydırık).
// KAZANÇ: Unity'deki gibi yatay altın bant (etiket_kazanc), üst-orta.
const kazancT = plaka(dokular.etiket_kazanc, GENISLIK / 2, 62, 560, 40, 0);
const bakiyeT = plaka(dokular.etiket_bakiye, 330, YUKSEKLIK - 78, 470, 36, 4);
bakiyeT.position.x += 70;
const bahisT = plaka(dokular.etiket_bahis, GENISLIK - 330, YUKSEKLIK - 78, 470, 36, 4);
bahisT.position.x += 55;

function metinleriGuncelle() {
  bakiyeT.text = `${bakiye.toLocaleString("tr-TR")} TL`;
  bahisT.text = `${bahis.toLocaleString("tr-TR")} TL`;
  kazancT.text = `KAZANÇ: ${kazancSon.toLocaleString("tr-TR")} TL`;
}
metinleriGuncelle();

// --- Alt-orta: [-] [SPIN] [+] + sağ-alt AYARLAR ---
const spinBtn = new PIXI.Sprite(dokular.btn_spin);
spinBtn.anchor.set(0.5);
spinBtn.position.set(GENISLIK / 2, YUKSEKLIK - 88);
spinBtn.scale.set(170 / spinBtn.texture.height);
spinBtn.eventMode = "static"; spinBtn.cursor = "pointer";
S.addChild(spinBtn);

const BAHISLER = [50, 100, 200, 300, 500, 1000, 1500, 2500, 4000];
function bahisBtn(doku, dx, yon) {
  const b = new PIXI.Sprite(doku);
  b.anchor.set(0.5);
  b.position.set(GENISLIK / 2 + dx, YUKSEKLIK - 85);
  b.scale.set(105 / b.texture.height);
  b.eventMode = "static"; b.cursor = "pointer";
  b.on("pointertap", () => {
    if (spinAktif) return;
    const i = BAHISLER.indexOf(bahis);
    bahis = BAHISLER[Math.min(BAHISLER.length - 1, Math.max(0, i + yon))];
    metinleriGuncelle();
  });
  S.addChild(b);
}
bahisBtn(dokular.btn_bahis_azalt, -150, -1);
bahisBtn(dokular.btn_bahis_artir, 150, +1);

const ayarBtn = new PIXI.Sprite(dokular.btn_ayarlar);
ayarBtn.anchor.set(0.5);
ayarBtn.position.set(GENISLIK - 78, YUKSEKLIK - 84);
ayarBtn.scale.set(130 / ayarBtn.texture.height);
ayarBtn.eventMode = "static"; ayarBtn.cursor = "pointer";
ayarBtn.on("pointertap", () => console.log("[F4] Ayarlar — panel F6'da"));
S.addChild(ayarBtn);

// --- Win feedback en üstte ---
const wf = new WinFeedback(uygulama);
S.addChild(wf.kok);

// --- Spin akışı ---
const ayar = { egilimYuzde: MODLAR.normal.egilim, minKat: 0, maksKat: 0,
               aktifSenaryo: "normal", zorluk: 6, maxReroll: 200 };

async function spinYap() {
  if (spinAktif || bakiye < bahis) return;
  spinAktif = true; spinBtn.alpha = 0.5;
  bakiye -= bahis; kazancSon = 0; metinleriGuncelle();

  const sonuc = spinUret(bahis, ayar, rng);
  slot.gridGoster(sonuc.baslangicGrid);
  for (const adim of sonuc.adimlar) {
    sesCal("tumble_pop", { ses: 0.6, pitchRasgele: true });
    await slot.tumbleAdimiOynat(adim);
    kazancSon += adim.kazanc; metinleriGuncelle();
  }
  bakiye += sonuc.nihai;
  kazancSon = sonuc.nihai; metinleriGuncelle();
  if (sonuc.nihai >= bahis * 2) await wf.goster(sonuc.nihai, bahis);
  spinAktif = false; spinBtn.alpha = 1;
}
spinBtn.on("pointertap", spinYap);

slot.gridGoster(izgaraDoldur(rng, {}));
console.log("[F4] Unity düzenli ekran hazır");
