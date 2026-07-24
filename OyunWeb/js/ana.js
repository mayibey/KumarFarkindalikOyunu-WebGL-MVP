// Giriş noktası — F4 başlangıcı: gerçek varlıklarla OYNANABİLİR demo.
// Arka plan + oyun tahtası + 6x5 meyve ızgarası + SPIN + bakiye/bahis (normal mod %65).
import { sahneKur, uygulama, GENISLIK, YUKSEKLIK } from "./cekirdek/sahne.js";
import { shimKur, kopruKaydet } from "./kopru/sendMessageShim.js";
import { SEMBOLLER, EKONOMI, MODLAR } from "../veri/sabitler.js";
import { spinUret } from "./motor/spinMotoru.js";
import { izgaraDoldur } from "./motor/doldurucu.js";
import { rngYap } from "./motor/rng.js";
import { SlotGorunum } from "./ui/slotGorunum.js";

shimKur();
kopruKaydet("SunumKoprusu", {
  SunumAsamaGit: (n) => console.log("[F4] SunumAsamaGit", n, "— F5'te bağlanacak"),
  SunumPanelGit: () => console.log("[F4] SunumPanelGit — F6'da bağlanacak"),
  SunumSesAyarla: (a) => console.log("[F4] SunumSesAyarla", a),
});

await sahneKur();

// --- Font ---
const font = new FontFace("LilitaOne", "url(varlik/font/LilitaOne-Regular.ttf)");
await font.load(); document.fonts.add(font);

// --- Varlık yükleme ---
const dokuAdlari = [...SEMBOLLER, "arkaplan_oyun", "oyun_tahtasi", "btn_spin", "logo_kumar_yazisi"];
const dokular = {};
await Promise.all(dokuAdlari.map(async (ad) => {
  dokular[ad] = await PIXI.Assets.load(`varlik/gorsel/${ad}.webp`);
}));

// --- Sahne kurulumu ---
const S = uygulama.stage;
const arka = new PIXI.Sprite(dokular.arkaplan_oyun);
arka.width = GENISLIK; arka.height = YUKSEKLIK;
S.addChild(arka);

const logo = new PIXI.Sprite(dokular.logo_kumar_yazisi);
logo.anchor.set(0.5, 0); logo.position.set(250, 20);
logo.scale.set(Math.min(420 / logo.texture.width, 1));
S.addChild(logo);

// Oyun tahtası (çerçeve) — merkez
const HUCRE = 132, BOSLUK = 10;
const gridW = 6 * HUCRE + 5 * BOSLUK, gridH = 5 * HUCRE + 4 * BOSLUK;
const tahta = new PIXI.Sprite(dokular.oyun_tahtasi);
tahta.anchor.set(0.5);
tahta.position.set(GENISLIK / 2, YUKSEKLIK / 2 - 40);
const tahtaOlcek = Math.max((gridW + 150) / tahta.texture.width, (gridH + 150) / tahta.texture.height);
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

const ayar = { egilimYuzde: MODLAR.normal.egilim, minKat: 0, maksKat: 0,
               aktifSenaryo: "normal", zorluk: 6, maxReroll: 200 };

async function spinYap() {
  if (spinAktif || bakiye < bahis) return;
  spinAktif = true; spinBtn.alpha = 0.5;
  bakiye -= bahis; kazancSon = 0; metinleriGuncelle();

  const sonuc = spinUret(bahis, ayar, rng);
  slot.gridGoster(sonuc.baslangicGrid);
  for (const adim of sonuc.adimlar) {
    await slot.tumbleAdimiOynat(adim);
    kazancSon += adim.kazanc; metinleriGuncelle();
  }
  bakiye += sonuc.nihai;
  kazancSon = sonuc.nihai; metinleriGuncelle();
  spinAktif = false; spinBtn.alpha = 1;
}
spinBtn.on("pointertap", spinYap);

// Açılış: nötr grid göster
slot.gridGoster(izgaraDoldur(rng, {}));
console.log("[F4] oynanabilir demo hazır");
