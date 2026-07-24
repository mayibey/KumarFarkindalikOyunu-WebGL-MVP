// Giriş noktası — F4: Unity 02 sahnesi düzenine göre oynanabilir ekran.
// Yerleşim referansı: Unity ekran görüntüleri (KAZANÇ plakası üst-orta, Bakiye sol-alt,
// Bahis sağ-alt, kırmızı -/+ ve SPIN alt-orta, AYARLAR çarkı sağ-alt).
import { sahneKur, uygulama, GENISLIK, YUKSEKLIK } from "./cekirdek/sahne.js";
import { shimKur, kopruKaydet } from "./kopru/sendMessageShim.js";
import { SEMBOLLER, EKONOMI, MODLAR, ASAMALAR } from "../veri/sabitler.js";
import { senaryoVerisiYukle, scriptedSpinBul, asamaScriptedSpinSayisi, kaydiPlanla } from "./motor/scriptedOynatici.js";
import { egitmenModal, karsilamaModallari } from "./ui/modalDom.js";
import { anlaticiAc, anlaticiKapat, anlaticiGuncelle, anlaticiKopruKur } from "./kopru/anlaticiKopru.js";
import { bonusTuzagiPopup, borcPaneli, finalEkrani } from "./ui/senaryoOverlaylar.js";
import { girisEkraniGoster } from "./ui/girisEkrani.js";
import { kaydet, yukle } from "./cekirdek/kayit.js";
import { panelAc, panelAyar, panelAyarIsle, panelKopruKur } from "./kopru/panelKopru.js";
import { spinUret } from "./motor/spinMotoru.js";
import { izgaraDoldur } from "./motor/doldurucu.js";
import { rngYap } from "./motor/rng.js";
import { SlotGorunum } from "./ui/slotGorunum.js";
import { WinFeedback } from "./ui/winFeedback.js";
import { sesYukle, sesCal, sesKilidiKur, anaSes } from "./cekirdek/ses.js";

shimKur();
kopruKaydet("SunumKoprusu", {
  SunumAsamaGit: (n) => senaryoBaslat(Math.max(0, Math.min(6, n | 0))),
  SunumPanelGit: () => panelModunaGec(),
  SunumSesAyarla: (a) => anaSes(a === 1 ? 1 : 0),
});
// panel.html → PanelKopru.AyarAl (shim yoneticiPanel mesajlarını buraya yönlendirir)
kopruKaydet("PanelKopru", { AyarAl: (json) => panelAyarIsle(json) });
panelKopruKur();
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

// --- Oyun tahtası + İÇİNE oturan ızgara ---
// Ö1+Ö2 (tur2): Unity'de grid dikeyde alanı DOLDURUR, semboller iri. Tahta büyütüldü.
const TAHTA_H = 880;
const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
tahta.anchor.set(0.5);
tahta.position.set(GENISLIK / 2, 500);
tahta.scale.set(TAHTA_H / tahta.texture.height);
S.addChild(tahta);

const tahtaW = tahta.texture.width * tahta.scale.x;
const icW = tahtaW * 0.85, icH = TAHTA_H * 0.72;
const BOSLUK = 8;
const HUCRE = Math.min((icW - 5 * BOSLUK) / 6, (icH - 4 * BOSLUK) / 5);
const gridW = 6 * HUCRE + 5 * BOSLUK, gridH = 5 * HUCRE + 4 * BOSLUK;

const slot = new SlotGorunum(uygulama, dokular, {
  x: tahta.position.x - gridW / 2, y: tahta.position.y - gridH / 2 + 10,
  hucre: HUCRE, bosluk: BOSLUK,
});
S.addChild(slot.kok);

// --- Hoş geldin kutusu (Unity birebir: silik koyu sade kutu, ince çerçeve, küçük punto + x) ---
const hosKutu = new PIXI.Graphics()
  .roundRect(-160, -26, 320, 52, 10)
  .fill({ color: 0x0e1a38, alpha: 0.85 })
  .stroke({ color: 0xd8a63a, width: 1.5 });
hosKutu.position.set(GENISLIK - 220, 54);
S.addChild(hosKutu);
// Ö4 (tur2): Unity serif ALTIN yazı (beyaz değil), ✕ kutu DIŞINDA sağ üst köşe
const hosT = new PIXI.Text({ text: "Hoş Geldiniz Misafir", style: {
  fontFamily: "Georgia, serif", fontSize: 21, fontWeight: "bold", fill: 0xe8c66a } });
hosT.anchor.set(0.5); hosT.position.set(GENISLIK - 220, 54);
S.addChild(hosT);
// Unity'deki küçük 'x' kapatma ikonu (kutu dışında sağ üst)
const hosX = new PIXI.Text({ text: "✕", style: {
  fontFamily: "Arial", fontSize: 17, fill: 0x9aa4b8 } });
hosX.anchor.set(0.5); hosX.position.set(GENISLIK - 46, 30);
hosX.eventMode = "static"; hosX.cursor = "pointer";
hosX.on("pointertap", () => { hosKutu.visible = false; hosT.visible = false; hosX.visible = false; });
S.addChild(hosX);
function hosGeldinGuncelle() { hosT.text = `Hoş Geldiniz ${senaryo.kullaniciAdi || "Misafir"}`; }

// --- Ekonomi durumu ---
const rng = rngYap((Math.floor(performance.now()) ^ 0x5eed) >>> 0);
let bakiye = EKONOMI.baslangicBakiye, bahis = 500, kazancSon = 0, spinAktif = false;

// --- Plakalar — Unity BİREBİR (workflow denetim Öncelik 1): DİK DÖRTGEN, koyu bordo
//     yarı-saydam zemin, ince altın çerçeve, etiket+değer AYNI SATIR tek sarı, karma harf. ---
// Ö1 (tur2): Unity plakaları YÜKSEK + kalın ÇİFT katmanlı altın çerçeve.
function altPlaka(x, y, genislik, yukseklik = 84) {
  const g = new PIXI.Graphics()
    .roundRect(-genislik / 2, -yukseklik / 2, genislik, yukseklik, 11)
    .fill({ color: 0x2a0d0d, alpha: 0.66 })
    .stroke({ color: 0xf0c860, width: 3.5 })                        // dış kalın açık altın
    .roundRect(-genislik / 2 + 6, -yukseklik / 2 + 6, genislik - 12, yukseklik - 12, 8)
    .stroke({ color: 0x7a5a16, width: 1.5 });                       // iç ince koyu (çift katman)
  g.position.set(x, y);
  S.addChild(g);
  const t = new PIXI.Text({ text: "", style: {
    fontFamily: "LilitaOne", fontSize: 38, fill: 0xffd94d,
    stroke: { color: 0x2a1800, width: 3 } } });
  t.anchor.set(0.5); t.position.set(x, y);
  S.addChild(t);
  return t;
}
// KAZANÇ: Unity yatay altın bant (etiket_kazanc); yazı BEYAZ (Öncelik 5), glow yok.
const kazancSp = new PIXI.Sprite(dokular.etiket_kazanc);
kazancSp.anchor.set(0.5); kazancSp.position.set(GENISLIK / 2, 62);
kazancSp.scale.set(560 / kazancSp.texture.width);
S.addChild(kazancSp);
const kazancT = new PIXI.Text({ text: "", style: {
  fontFamily: "LilitaOne", fontSize: 40, fill: 0xfdf6e3,
  stroke: { color: 0x3a2400, width: 4 } } });
kazancT.anchor.set(0.5); kazancT.position.set(GENISLIK / 2, 62);
S.addChild(kazancT);

const bakiyeT = altPlaka(340, YUKSEKLIK - 76, 500);
const bahisT = altPlaka(GENISLIK - 340, YUKSEKLIK - 76, 500);

function metinleriGuncelle() {
  bakiyeT.text = `Bakiye: ${bakiye.toLocaleString("tr-TR")} TL`;
  bahisT.text = `Bahis: ${bahis.toLocaleString("tr-TR")} TL`;
  kazancT.text = `KAZANÇ: ${kazancSon.toLocaleString("tr-TR")} TL`;
}
metinleriGuncelle();

// --- Alt-orta: [-] [SPIN] [+] + sağ-alt AYARLAR ---
// Ö2 (tur2): Unity'de butonlar BÜYÜK, küme kompakt (aralar dar).
const spinBtn = new PIXI.Sprite(dokular.btn_spin);
spinBtn.anchor.set(0.5);
spinBtn.position.set(GENISLIK / 2, YUKSEKLIK - 82);
spinBtn.scale.set(192 / spinBtn.texture.height);
spinBtn.eventMode = "static"; spinBtn.cursor = "pointer";
S.addChild(spinBtn);

const BAHISLER = [50, 100, 200, 300, 500, 1000, 1500, 2500, 4000];
function bahisBtn(doku, dx, yon) {
  const b = new PIXI.Sprite(doku);
  b.anchor.set(0.5);
  b.position.set(GENISLIK / 2 + dx, YUKSEKLIK - 82);
  b.scale.set(128 / b.texture.height);
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

// --- Senaryo durumu (F5): aşama 0-6, scripted A0-A4 + dinamik eğilim aşamaları ---
const senaryoVeri = await senaryoVerisiYukle();
const senaryo = { aktif: false, asama: 0, spin: 1, toplamSpin: 0,
                  spinNetleri: [], asamaBasBakiye: 0,
                  bonusModu: false, bonusSpin: 0, borcAlindi: false, toplamYatirim: 0 };

function anlaticiDurumGonder(ek = {}) {
  anlaticiGuncelle({
    asama: senaryo.asama, spin: senaryo.spin - 1,
    hedefSpin: ASAMALAR.spinHedefi[senaryo.asama],
    bakiyeNet: bakiye - senaryo.asamaBasBakiye,
    toplamSpin: senaryo.toplamSpin,
    spinNetleri: senaryo.spinNetleri.slice(),
    tukenis: false, ...ek,
  });
}

async function senaryoBaslat(asamaIdx, karsilama = false) {
  senaryo.aktif = true;
  senaryo.asama = asamaIdx;
  senaryo.spin = 1;
  senaryo.spinNetleri = [];
  senaryo.asamaBasBakiye = bakiye;
  bahis = ASAMALAR.bahisler[asamaIdx];
  metinleriGuncelle();
  hosGeldinGuncelle();
  anlaticiAc();
  anlaticiDurumGonder();
  // Baştan başlarken (aşama 0, karşılama isteniyorsa) 3 tanıtım modalı
  if (karsilama && asamaIdx === 0) await karsilamaModallari();
  console.log(`[F5] senaryo aşama ${asamaIdx + 1} başladı (bahis ${bahis})`);
}

anlaticiKopruKur({
  asamaDegis: (n) => { if (Number.isFinite(n)) senaryoBaslat(Math.max(0, Math.min(6, n))); },
  yenidenBaslat: () => { bakiye = EKONOMI.baslangicBakiye; senaryo.toplamSpin = 0; senaryoBaslat(0); },
});

// Serbest/panel mod ayarı = panelAyar (panel canlı günceller)
const ayar = panelAyar;

// Manipülasyon paneli moduna geç: senaryo kapat, ANLATICI ŞERİDİ GİZLE (Öncelik 3b), paneli aç
function panelModunaGec() {
  senaryo.aktif = false;
  const g = document.getElementById("girisEkraniKok"); if (g) g.remove();
  anlaticiKapat();                 // panel modunda sol senaryo şeridi OLMAMALI (Unity birebir)
  panelAc();
  console.log("[F6] manipülasyon paneli açıldı");
}

function senaryoDinamikAyar() {
  const a = senaryo.asama;
  return { egilimYuzde: ASAMALAR.egilimler[a], minKat: 0, maksKat: 0,
           aktifSenaryo: "normal", zorluk: 8 - Math.round((ASAMALAR.egilimler[a] - 50) / 25),
           maxReroll: 500 };
}

async function planOynat(plan) {
  slot.gridGoster(plan.baslangicGrid, plan.baslangicCarpan);
  for (const adim of plan.adimlar) {
    sesCal("tumble_pop", { ses: 0.6, pitchRasgele: true });
    await slot.tumbleAdimiOynat(adim);
    kazancSon += adim.kazanc; metinleriGuncelle();
  }
}

async function spinYap() {
  if (spinAktif || bakiye < bahis) return;
  spinAktif = true; spinBtn.alpha = 0.5;
  bakiye -= bahis; kazancSon = 0; metinleriGuncelle();

  senaryo.toplamYatirim += bahis;
  let nihai = 0, modal = null, bonusTetik = false;
  if (senaryo.aktif) {
    const kayit = scriptedSpinBul(senaryo.asama, senaryo.spin);
    if (kayit) {
      const plan = kaydiPlanla(kayit);
      await planOynat(plan);
      nihai = plan.nihai;
      modal = kayit.modal;
      bonusTetik = !!kayit.bonusTetik;
    } else {
      const sonuc = spinUret(bahis, senaryoDinamikAyar(), rng);
      await planOynat({ baslangicGrid: sonuc.baslangicGrid, baslangicCarpan: null,
                        adimlar: sonuc.adimlar, nihai: sonuc.nihai });
      nihai = sonuc.nihai;
    }
  } else {
    const sonuc = spinUret(bahis, ayar, rng);
    await planOynat({ baslangicGrid: sonuc.baslangicGrid, baslangicCarpan: null,
                      adimlar: sonuc.adimlar, nihai: sonuc.nihai });
    nihai = sonuc.nihai;
  }

  bakiye += nihai;
  kazancSon = nihai; metinleriGuncelle();
  if (nihai >= bahis * 2) await wf.goster(nihai, bahis);
  if (modal) await egitmenModal(modal);

  if (senaryo.aktif) {
    senaryo.spin++;
    senaryo.toplamSpin++;
    senaryo.spinNetleri.push(nihai - bahis);

    // A4S4 bonus tuzağı: cazip popup → BONUS AL → 10 sabit bonus spini (4000 TL) → bakiye erir
    if (bonusTetik) { await bonusAkisiOynat(); }

    const hedef = ASAMALAR.spinHedefi[senaryo.asama];
    const sonAsama = senaryo.asama >= 6;

    // Borç paneli: bakiye bitti + son aşamaya yaklaşıldıysa (A5→A6 köprüsü)
    if (bakiye < bahis && !senaryo.borcAlindi && senaryo.asama >= 4 && !sonAsama) {
      await borcPaneli();
      bakiye += EKONOMI.borcMiktari; senaryo.borcAlindi = true;
      metinleriGuncelle();
    }

    if (senaryo.spin > hedef && !sonAsama) {
      senaryo.asama++;
      senaryo.spin = 1;
      senaryo.spinNetleri = [];
      senaryo.asamaBasBakiye = bakiye;
      bahis = ASAMALAR.bahisler[senaryo.asama];
    }
    anlaticiDurumGonder();
    metinleriGuncelle();
    senaryoKaydet();

    // A7 tükeniş: son aşamada bakiye tükendiyse final cutscene
    if (senaryo.asama >= 6 && bakiye < bahis) {
      const yatirim = EKONOMI.baslangicBakiye + (senaryo.borcAlindi ? EKONOMI.borcMiktari : 0);
      finalEkrani({
        toplamYatirim: yatirim, sonBakiye: bakiye, netKayip: yatirim - bakiye,
        toplamSpin: senaryo.toplamSpin,
      }, () => location.reload());
    }
  }
  spinAktif = false; spinBtn.alpha = 1;
}

// Bonus akışı: popup + 10 sabit bonus spini (senaryo.json bonusSpinleri, toplam 4000 TL).
async function bonusAkisiOynat() {
  await bonusTuzagiPopup();
  for (const kayit of senaryoVeri.bonusSpinleri) {
    const plan = kaydiPlanla(kayit);
    await planOynat(plan);
    bakiye += plan.nihai;
    kazancSon = plan.nihai; metinleriGuncelle();
    await bekleKisa(300);
  }
}
const bekleKisa = (ms) => new Promise((r) => setTimeout(r, ms));
spinBtn.on("pointertap", spinYap);

slot.gridGoster(izgaraDoldur(rng, {}));

// Otomatik kaydet yardımı (senaryo modunda spin/aşama sonrası)
function senaryoKaydet() {
  if (!senaryo.aktif) return;
  kaydet({ kullaniciAdi: senaryo.kullaniciAdi, asama: senaryo.asama, spin: senaryo.spin,
           toplamSpin: senaryo.toplamSpin, bakiye, borcAlindi: senaryo.borcAlindi,
           toplamYatirim: senaryo.toplamYatirim });
}

function senaryoDevamEt(k) {
  senaryo.kullaniciAdi = k?.kullaniciAdi || "Misafir";
  bakiye = k?.sonBakiye ?? EKONOMI.baslangicBakiye;
  senaryo.toplamSpin = k?.toplamSpin ?? 0;
  senaryo.borcAlindi = k?.borcAlindi ?? false;
  senaryo.toplamYatirim = k?.toplamYatirim ?? 0;
  senaryoBaslat(k?.aktifAsama ?? 0);
  senaryo.spin = k?.aktifSpin ?? 1;
  anlaticiDurumGonder(); metinleriGuncelle();
}

// --- Açılış akışı ---
if (location.search.indexOf("senaryo") >= 0) {
  senaryo.kullaniciAdi = "Misafir"; senaryoBaslat(0, true);
} else if (location.search.indexOf("panel") >= 0) {
  panelModunaGec();
} else {
  girisEkraniGoster({
    onSenaryo: (ad) => { senaryo.kullaniciAdi = ad; senaryoBaslat(0, true); },
    onDevam: (k) => senaryoDevamEt(k),
    onPanel: () => window.unityInstance.SendMessage("SunumKoprusu", "SunumPanelGit", 0),
  });
}
console.log("[F5] açılış akışı hazır");
