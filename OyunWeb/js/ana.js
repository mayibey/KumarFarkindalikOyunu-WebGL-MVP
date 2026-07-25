// Giriş noktası — F4: Unity 02 sahnesi düzenine göre oynanabilir ekran.
// Yerleşim referansı: Unity ekran görüntüleri (KAZANÇ plakası üst-orta, Bakiye sol-alt,
// Bahis sağ-alt, kırmızı -/+ ve SPIN alt-orta, AYARLAR çarkı sağ-alt).
import { sahneKur, uygulama, GENISLIK, YUKSEKLIK } from "./cekirdek/sahne.js";
import { shimKur, kopruKaydet } from "./kopru/sendMessageShim.js";
import { SEMBOLLER, EKONOMI, MODLAR, ASAMALAR } from "../veri/sabitler.js";
import { senaryoVerisiYukle, scriptedSpinBul, asamaScriptedSpinSayisi, kaydiPlanla } from "./motor/scriptedOynatici.js";
import { egitmenModal, karsilamaModallari, asistanModal } from "./ui/modalDom.js";
import { anlaticiAc, anlaticiKapat, anlaticiGuncelle, anlaticiKopruKur } from "./kopru/anlaticiKopru.js";
import { bonusTuzagiPopup, borcPaneli, finalEkrani } from "./ui/senaryoOverlaylar.js";
import { girisEkraniKur } from "./ui/girisEkrani.js";
import { kaydet, yukle } from "./cekirdek/kayit.js";
import { panelAc, panelAyar, panelAyarIsle, panelKopruKur } from "./kopru/panelKopru.js";
import { bahisPaneliAc, bahisKopruKur } from "./kopru/bahisKopru.js";
import { spinUret } from "./motor/spinMotoru.js";
import { izgaraDoldur } from "./motor/doldurucu.js";
import { rngYap } from "./motor/rng.js";
import { SlotGorunum } from "./ui/slotGorunum.js";
import { WinFeedback } from "./ui/winFeedback.js";
import { sesYukle, sesCal, sesKilidiKur, anaSes, fonMuzigiCal } from "./cekirdek/ses.js";

shimKur();
kopruKaydet("SunumKoprusu", {
  SunumAsamaGit: (n) => senaryoBaslat(Math.max(0, Math.min(6, n | 0))),
  SunumPanelGit: () => panelModunaGec(),
  SunumSesAyarla: (a) => anaSes(a === 1 ? 1 : 0),
});
// panel.html → PanelKopru.AyarAl (shim yoneticiPanel mesajlarını buraya yönlendirir)
kopruKaydet("PanelKopru", { AyarAl: (json) => panelAyarIsle(json) });
panelKopruKur();
bahisKopruKur();
sesKilidiKur();

await sahneKur();

const font = new FontFace("LilitaOne", "url(varlik/font/LilitaOne-Regular.ttf)");
await font.load(); document.fonts.add(font);
// LilitaOne'da Ş İ Ğ ş ğ glyph'leri YOK → TitanOne (LilitaOne'a görsel özdeş) fallback ile
// eksik Türkçe karakterleri tamamla. Tüm PIXI.Text fontFamily "LilitaOne, TitanOne, sans-serif".
const fontTR = new FontFace("TitanOne", "url(varlik/font/TitanOne-Regular.ttf)");
await fontTR.load(); document.fonts.add(fontTR);
const FONT_YIGIN = "LilitaOne, TitanOne, sans-serif";

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
]);
// Fon müziği: giriş ekranında ÇALMAZ, sadece oyuna/panele girince başlar.
let _muzikBasladi = false;
function muzikBaslat() {
  if (_muzikBasladi) return;
  _muzikBasladi = true;
  fonMuzigiCal("varlik/ses/fon_muzigi.mp3", 0.25);
}

const S = uygulama.stage;

// --- Arka plan + logo ---
const arka = new PIXI.Sprite(dokular.arkaplan_oyun);
arka.width = GENISLIK; arka.height = YUKSEKLIK;
S.addChild(arka);

// Unity Logo RectTransform: anchor(0,1) pivot(0,1), anchoredPos(35, 38.28), sizeDelta(441,337)
// → sol-üst köşe ekranda (35, -38), genişlik 441 (ekran üstünden hafif taşar, Unity birebir).
const logo = new PIXI.Sprite(dokular.logo_kumar_yazisi);
logo.anchor.set(0, 0); logo.position.set(35, -38);
logo.scale.set(441 / logo.texture.width);
S.addChild(logo);

// --- Izgara ÖNCE (sık + iri kare hücre), TAHTA grid'i SARAR (Unity düzeni) ---
// Kullanıcı: meyveler seyrek olmasın; büyük ve sık, tahta grid'e otursun.
// Unity SlotGrid (02_SenaryoluOyun.unity): cellSize 165x120, spacing 5, 6 sütun × 5 satır.
// Merkez anchoredPos(94, 35.6) → ekran (1054, 504). Hücre kutusu 120 (kare meyve), sütun
// aralığı 170 (165+5), satır aralığı 125 (120+5) → grid Unity'de GENİŞ+BASIK, sağda, aşağıda.
const gridMerkez = { x: 1054, y: 504 };
const HUCRE = 120, BOSLUK_X = 50, BOSLUK_Y = 5;
const gridW = 6 * HUCRE + 5 * BOSLUK_X, gridH = 5 * HUCRE + 4 * BOSLUK_Y;

// Tahta iç alan oranı (oyun_tahtasi.webp): iç-w ~%79, iç-h ~%55.5. Tahtayı grid'i saracak
// biçimde NON-UNIFORM ölçekle + %8 KENAR PAYI (grid tahtadan içeride, meyveler çerçeveye değmez).
// Unity tahtası daha KOMPAKT/DİK: iç alan oranları grid'e göre ayarlı, hafif kenar payı.
// Unity "meyvelerarkaplanı" RectTransform: anchoredPos(94,26) → merkez (1054,514),
// sizeDelta 1339x1215. İç koyu alan (%76x%55) grid'i (970x620) rahat sarar → meyve taşmaz.
const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
tahta.anchor.set(0.5);
tahta.position.set(1054, 514);
tahta.scale.set(1339 / tahta.texture.width, 1215 / tahta.texture.height);
S.addChild(tahta);

const slot = new SlotGorunum(uygulama, dokular, {
  x: gridMerkez.x - gridW / 2, y: gridMerkez.y - gridH / 2,
  hucre: HUCRE, boslukX: BOSLUK_X, boslukY: BOSLUK_Y,
});
S.addChild(slot.kok);
// Maske ÜSTTE genişletildi: meyveler grid tepesinden düşerken görünür (tumble dökülme).
const maskUstPay = HUCRE * 1.3;
const slotMaske = new PIXI.Graphics()
  .rect(gridMerkez.x - gridW / 2 - 4, gridMerkez.y - gridH / 2 - maskUstPay, gridW + 8, gridH + maskUstPay + 4)
  .fill(0xffffff);
S.addChild(slotMaske);
slot.kok.mask = slotMaske;

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
// Ö1 (tur2+3): Unity plakaları BÜYÜK + kalın ÇİFT katmanlı altın çerçeve, İRİ sarı yazı.
function altPlaka(x, y, genislik, yukseklik = 92) {
  const g = new PIXI.Graphics()
    .roundRect(-genislik / 2, -yukseklik / 2, genislik, yukseklik, 12)
    .fill({ color: 0x2a0d0d, alpha: 0.68 })
    .stroke({ color: 0xf0c860, width: 4 })                          // dış kalın açık altın
    .roundRect(-genislik / 2 + 7, -yukseklik / 2 + 7, genislik - 14, yukseklik - 14, 8)
    .stroke({ color: 0x7a5a16, width: 1.5 });                       // iç ince koyu (çift katman)
  g.position.set(x, y);
  S.addChild(g);
  const t = new PIXI.Text({ text: "", style: {
    fontFamily: FONT_YIGIN, fontSize: 44, fill: 0xf5f006,   // Unity ölçümü: saf parlak sarı (245,240,8)
    dropShadow: { color: 0x000000, alpha: 0.5, blur: 2, distance: 2 } } }); // stroke yerine gölge (renk saf kalsın)
  t.anchor.set(0.5); t.position.set(x, y);
  S.addChild(t);
  return t;
}
// KAZANÇ: Unity SpinKazancText ekran merkez (1033,58) — grid ile hizalı (ortada değil, sağda).
const kazancSp = new PIXI.Sprite(dokular.etiket_kazanc);
kazancSp.anchor.set(0.5); kazancSp.position.set(1033, 58);
kazancSp.scale.set(560 / kazancSp.texture.width);
S.addChild(kazancSp);
const kazancT = new PIXI.Text({ text: "", style: {
  fontFamily: FONT_YIGIN, fontSize: 40, fill: 0xf2d605,   // Unity ölçümü: parlak sarı (242,214,5)
  dropShadow: { color: 0x000000, alpha: 0.5, blur: 2, distance: 2 } } });
kazancT.anchor.set(0.5); kazancT.position.set(1033, 58);
S.addChild(kazancT);

// Unity BakiyeGorsel anchoredPos(-461,-436) size(490,160) → (499,976); BahisGorsel(487,-436)→(1447,976).
const bakiyeT = altPlaka(499, 976, 490, 160);
const bahisT = altPlaka(1447, 976, 490, 160);
const bahisTiklaAlan = new PIXI.Graphics()
  .rect(1447 - 245, 976 - 80, 490, 160).fill({ color: 0xffffff, alpha: 0.001 });
bahisTiklaAlan.eventMode = "static"; bahisTiklaAlan.cursor = "pointer";
bahisTiklaAlan.on("pointertap", () => {
  if (!spinAktif) bahisPaneliAc(bakiye, (m) => { if (m > 0) { bahis = m; metinleriGuncelle(); } });
});
S.addChild(bahisTiklaAlan);

function metinleriGuncelle() {
  bakiyeT.text = `Bakiye: ${bakiye.toLocaleString("tr-TR")} TL`;
  bahisT.text = `Bahis: ${bahis.toLocaleString("tr-TR")} TL`;
  kazancT.text = `KAZANÇ: ${kazancSon.toLocaleString("tr-TR")} TL`;
}
metinleriGuncelle();

// --- Alt-orta: [-] [SPIN] [+] + sağ-alt AYARLAR ---
// Ö2 (tur2): Unity'de butonlar BÜYÜK, küme kompakt (aralar dar).
// Unity: SPIN merkez (960,976); bahisAzalt (813,976) size~139x160; bahisArttir (1126,976) ~152x160.
// Unity SpinIcon ekran merkez (964,989), boyut 370x271 (YAML delili).
const spinBtn = new PIXI.Sprite(dokular.btn_spin);
spinBtn.anchor.set(0.5);
spinBtn.position.set(964, 989);
spinBtn.scale.set(271 / spinBtn.texture.height);   // Unity SpinIcon yüksekliği 271
spinBtn.eventMode = "static"; spinBtn.cursor = "pointer";
S.addChild(spinBtn);

const BAHISLER = [50, 100, 200, 300, 500, 1000, 1500, 2500, 4000];
function bahisBtn(doku, x, yon) {
  const b = new PIXI.Sprite(doku);
  b.anchor.set(0.5);
  b.position.set(x, 976);
  b.scale.set(160 / b.texture.height);             // Unity buton yüksekliği 160
  b.eventMode = "static"; b.cursor = "pointer";
  b.on("pointertap", () => {
    if (spinAktif) return;
    const i = BAHISLER.indexOf(bahis);
    bahis = BAHISLER[Math.min(BAHISLER.length - 1, Math.max(0, i + yon))];
    metinleriGuncelle();
  });
  S.addChild(b);
}
bahisBtn(dokular.btn_bahis_azalt, 813, -1);
bahisBtn(dokular.btn_bahis_artir, 1126, +1);

// Unity AyarlarButton: sağ-alt köşe (1897.7,1053) size(176,155) → merkez (1810, 976).
const ayarBtn = new PIXI.Sprite(dokular.btn_ayarlar);
ayarBtn.anchor.set(0.5);
ayarBtn.position.set(1810, 976);
ayarBtn.scale.set(155 / ayarBtn.texture.height);
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
  // Baştan başlarken (aşama 0, karşılama isteniyorsa): grid düşerek gelir + 3 tanıtım modalı
  if (karsilama && asamaIdx === 0) {
    await slot.gridDusereksGoster(izgaraDoldur(rng, {}));  // meyveler yukarıdan dökülür (Unity hissi)
    await karsilamaModallari();
  }
  console.log(`[F5] senaryo aşama ${asamaIdx + 1} başladı (bahis ${bahis})`);
}

anlaticiKopruKur({
  asamaDegis: (n) => { if (Number.isFinite(n)) senaryoBaslat(Math.max(0, Math.min(6, n))); },
  yenidenBaslat: () => { bakiye = EKONOMI.baslangicBakiye; senaryo.toplamSpin = 0; senaryoBaslat(0); },
});

// Serbest/panel mod ayarı = panelAyar (panel canlı günceller)
const ayar = panelAyar;

// Manipülasyon paneli moduna geç: senaryo kapat, şerit gizle, panel aç + Unity tanıtım modalı
async function panelModunaGec() {
  senaryo.aktif = false;
  anlaticiKapat();                 // panel modunda sol senaryo şeridi OLMAMALI (Unity birebir)
  hosKutu.visible = false; hosT.visible = false; hosX.visible = false; // Unity panelinde hoş geldin YOK
  bahis = 500; metinleriGuncelle();
  panelAc();
  await slot.gridDusereksGoster(izgaraDoldur(rng, {}));  // panele girince meyveler düşerek gelir
  console.log("[F6] manipülasyon paneli açıldı");
  // Unity'de panele geçince gösterilen tanıtım modalı ("yeni sayfa" hissi) — metin panel_unity'den
  await asistanModal("BİLGİLENDİRİCİ ASİSTAN",
    `Bu bölüm, kumar yazılımlarının perde arkasını kendi elinizle keşfedebileceğiniz bir
    <span style="color:#60a5fa">test alanıdır</span>.<br><br>
    Soldaki yönetici panelinden makinenin davranışını siz belirlersiniz:
    <span style="color:#60a5fa">ödeme aralığını</span> (kazancın bahsin kaç katı olacağı),
    <span style="color:#60a5fa">kazanç eğilimini</span> (sistemin ne sıklıkta kazandıracağını) ve
    <span style="color:#f4d678">bonus davranışını</span> ayarlayabilirsiniz.<br><br>
    Farklı oyun modlarını deneyerek, gerçek kumar sitelerinin oyuncuyu nasıl yönlendirdiğini
    gözlemleyin. Düşük ödeme eğilimiyle nasıl <span style="color:#ef4444">kayıp yaşatıldığını</span>,
    ya da <span style="color:#ef4444">bonus tuzaklarının</span> nasıl kurulduğunu görün.`);
}

function senaryoDinamikAyar() {
  const a = senaryo.asama;
  return { egilimYuzde: ASAMALAR.egilimler[a], minKat: 0, maksKat: 0,
           aktifSenaryo: "normal", zorluk: 8 - Math.round((ASAMALAR.egilimler[a] - 50) / 25),
           maxReroll: 500 };
}

async function planOynat(plan) {
  await slot.gridDusereksGoster(plan.baslangicGrid, plan.baslangicCarpan);  // spin başı: grid düşerek gelir
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
  muzikBaslat();
  senaryo.kullaniciAdi = "Misafir"; senaryoBaslat(0, true);
} else if (location.search.indexOf("panel") >= 0) {
  muzikBaslat();
  panelModunaGec();
} else {
  const girisKok = girisEkraniKur(uygulama, dokular, {
    onSenaryo: (ad) => { muzikBaslat(); senaryo.kullaniciAdi = ad; senaryoBaslat(0, true); },
    onDevam: (k) => { muzikBaslat(); senaryoDevamEt(k); },
    onPanel: () => { muzikBaslat(); panelModunaGec(); },
  });
  S.addChild(girisKok);
}
console.log("[F5] açılış akışı hazır");
