// Giriş noktası — F4 başlangıcı: gerçek varlıklarla OYNANABİLİR demo.
// Arka plan + oyun tahtası + 6x5 meyve ızgarası + SPIN + bakiye/bahis (normal mod %65).
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
  SunumSesAyarla: (a) => anaSes(a === 1 ? 1 : 0), // AudioListener.volume karşılığı
});
sesKilidiKur();

await sahneKur();

// --- Font ---
const font = new FontFace("LilitaOne", "url(varlik/font/LilitaOne-Regular.ttf)");
await font.load(); document.fonts.add(font);

// --- Varlık yükleme ---
const dokuAdlari = [...SEMBOLLER, "arkaplan_oyun", "oyun_tahtasi", "btn_spin",
  "logo_kumar_yazisi", "btn_bahis_artir", "btn_bahis_azalt"];
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

// --- Sahne kurulumu ---
const S = uygulama.stage;
const arka = new PIXI.Sprite(dokular.arkaplan_oyun);
arka.width = GENISLIK; arka.height = YUKSEKLIK;
S.addChild(arka);

const logo = new PIXI.Sprite(dokular.logo_kumar_yazisi);
logo.anchor.set(0.5, 0); logo.position.set(250, 20);
logo.scale.set(Math.min(420 / logo.texture.width, 1));
S.addChild(logo);

// Oyun tahtası (çerçeve) — merkez; grid çerçevenin İÇİNE oturur
const HUCRE = 118, BOSLUK = 10;
const gridW = 6 * HUCRE + 5 * BOSLUK, gridH = 5 * HUCRE + 4 * BOSLUK;
const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
tahta.anchor.set(0.5);
tahta.position.set(GENISLIK / 2, YUKSEKLIK / 2 - 40);
const tahtaOlcek = Math.max((gridW + 190) / tahta.texture.width, (gridH + 190) / tahta.texture.height);
tahta.scale.set(tahtaOlcek);
S.addChild(tahta);

const slot = new SlotGorunum(uygulama, dokular, {
  x: GENISLIK / 2 - gridW / 2, y: YUKSEKLIK / 2 - 40 - gridH / 2,
  hucre: HUCRE, bosluk: BOSLUK,
});
S.addChild(slot.kok);

// --- Ekonomi + metinler ---
const rng = rngYap((Math.floor(performance.now()) ^ 0x5eed) >>> 0);
let bakiye = EKONOMI.baslangicBakiye, bahis = 500, kazancSon = 0, spinAktif = false;

const stil = (boyut, renk = 0xf4d678) => ({ fontFamily: "LilitaOne", fontSize: boyut, fill: renk });
const bakiyeT = new PIXI.Text({ text: "", style: stil(44) });
bakiyeT.position.set(60, YUKSEKLIK - 90);
const bahisT = new PIXI.Text({ text: "", style: stil(44) });
bahisT.anchor.set(1, 0); bahisT.position.set(GENISLIK - 60, YUKSEKLIK - 90);
const kazancT = new PIXI.Text({ text: "", style: stil(48, 0xffd700) });
kazancT.anchor.set(0.5, 0); kazancT.position.set(GENISLIK / 2, 24);
S.addChild(bakiyeT, bahisT, kazancT);

function metinleriGuncelle() {
  bakiyeT.text = `Bakiye: ${bakiye.toLocaleString("tr-TR")} TL`;
  bahisT.text = `Bahis: ${bahis.toLocaleString("tr-TR")} TL`;
  kazancT.text = `KAZANÇ: ${kazancSon.toLocaleString("tr-TR")} TL`;
}
metinleriGuncelle();

// --- SPIN butonu ---
const spinBtn = new PIXI.Sprite(dokular.btn_spin);
spinBtn.anchor.set(0.5);
spinBtn.position.set(GENISLIK / 2, YUKSEKLIK - 70);
spinBtn.scale.set(Math.min(130 / spinBtn.texture.height, 1));
spinBtn.eventMode = "static"; spinBtn.cursor = "pointer";
S.addChild(spinBtn);

// --- Bahis +/- butonları ---
const BAHISLER = [50, 100, 200, 300, 500, 1000, 1500, 2500, 4000];
function bahisBtnYap(doku, dx, yon) {
  const b = new PIXI.Sprite(doku);
  b.anchor.set(0.5);
  b.position.set(GENISLIK / 2 + dx, YUKSEKLIK - 70);
  b.scale.set(Math.min(84 / b.texture.height, 1));
  b.eventMode = "static"; b.cursor = "pointer";
  b.on("pointertap", () => {
    if (spinAktif) return;
    const i = BAHISLER.indexOf(bahis);
    const yeni = BAHISLER[Math.min(BAHISLER.length - 1, Math.max(0, i + yon))];
    bahis = yeni; metinleriGuncelle();
  });
  S.addChild(b);
  return b;
}
bahisBtnYap(dokular.btn_bahis_azalt, -160, -1);
bahisBtnYap(dokular.btn_bahis_artir, 160, +1);

const wf = new WinFeedback(uygulama);
S.addChild(wf.kok); // en üstte

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
  else if (sonuc.nihai === 0 && sonuc.adimlar.length === 0 && Math.random() < 0.15)
    sesCal("kayip_horn", { ses: 0.3 });
  spinAktif = false; spinBtn.alpha = 1;
}
spinBtn.on("pointertap", spinYap);

// Açılış: nötr grid göster
slot.gridGoster(izgaraDoldur(rng, {}));
console.log("[F4] oynanabilir demo hazır");
