// Unity kaynağından BİREBİR alınan çekirdek sabitler. Her blokta kaynak dosya:satır.
// Değişiklik kuralı: önce Unity kaynağını doğrula, sonra burayı güncelle.

// --- Sembol eşlemesi: 02_SenaryoluOyun.unity:7832 sembolSpriteListesi sırası ---
export const SEMBOLLER = [
  "sembol_armut",           // 0 armut.png
  "sembol_cilek",           // 1 çilleekkk.png
  "sembol_erik",            // 2 errriklerrrr.png
  "sembol_hindistancevizi", // 3
  "sembol_karpuz",          // 4
  "sembol_muz",             // 5
  "sembol_elma",            // 6 elmalarrrr.png (DİKKAT: elmalar.png DEĞİL)
  "sembol_uzum",            // 7 üzzzümmmm.png (premium ödeme)
  "sembol_yildiz",          // 8 SCATTER
];
export const SCATTER_INDEX = 8;      // 02 sahne override (TumbleAyarlari default 7 EZİLİYOR)
export const MIN_CLUSTER = 8;        // TumbleAyarlari.cs:11 + sahne:11785
export const CARPAN_SEMBOL = -2;     // OyunYoneticisi.Fields — çarpan hücresi
export const SUTUN = 6, SATIR = 5;

// --- Ödeme tabloları: TumbleAyarlari.cs:16-22 (bahis çarpanı; index = sembol id) ---
// Scatter girdileri Awake'te 0'lanır (TumbleAyarlari.cs:72-76) → index 8 = 0.
export const ODEME_8_9   = [0.2, 0.3, 0.4, 0.5, 0.6, 0.8, 1.0, 1.5, 0];
export const ODEME_10_11 = [0.5, 0.6, 0.8, 1.0, 1.5, 2.0, 3.0, 5.0, 0];
export const ODEME_12P   = [1.0, 1.5, 2.0, 2.5, 3.0, 5.0, 10.0, 25.0, 0];
// Hesap kuralı (TumbleAyarlari.cs:100-149): sembol başına sayım; count<minPay atla;
// count<=7 (küçük cluster izinliyse) → ODEME_8_9[sym] * 0.5; <=9 → ODEME_8_9;
// <=11 → ODEME_10_11; 12+ → ODEME_12P. total += pay*bahis; RoundToInt.

// --- Animasyon süreleri: TumbleAyarlari.cs:151-162 ---
export const ANIM = { pop: 0.38, dusme: 0.82, adimArasi: 0.18, ustOffset: 120 };

// --- Çarpan: envanter (OyunYoneticisi.Fields.cs:505-570, CarpanServisi) ---
export const CARPAN = { uretimYuzde: 2, maxAdet: 5, toplama: "SUM", forceDegerler: [5, 10, 50, 100] };

// --- Mod preset'leri: PanelKopru.cs:490-559 (egilim %, minKat, maksKat) ---
export const MODLAR = {
  normal: { egilim: 65, min: 0,   maks: 0   },
  hook:   { egilim: 90, min: 1.1, maks: 2.2 },
  yontma: { egilim: 70, min: 0.3, maks: 0.7 },
  tutma:  { egilim: 15, min: 1.1, maks: 1.5, dongu: "2kayip1kazanc" },
  koruma: { egilim: 8,  min: 0.1, maks: 0.3 },
  ozel:   { egilim: 65, min: 0,   maks: 0,  manuel: true },
};

// --- Anlatıcı aşama tablosu: AnlaticiSeritKopru.cs:239-265 ---
export const ASAMALAR = {
  egilimler:   [95, 80, 65, 40, 25, 12, 5],   // kaynak: _asamalar (kademeli 95→5; F5'te birebir teyit)
  maxCarpan:   [5.0, 3.5, 2.5, 1.5, 1.0, 0.5, 0.1],
  bahisler:    [500, 1500, 1500, 2500, 4000, 10000, 1500],
  spinHedefi:  [8, 8, 8, 5, 4, 5, 999],
};
// NOT: egilimler/maxCarpan ara değerleri F5'te AnlaticiSeritKopru.cs:257-265'ten
// SAYI SAYI doğrulanacak (envanter yalnız uçları verdi: %95/5.0 → %5/0.1).

// --- Reroll bütçeleri: Spin.cs:321 AsamaIcinMaxReroll (20-2000), SIMULASYON_MAX_REROLL=28 ---
export const REROLL = { simulasyonMax: 28, asamaAralik: [20, 2000] };

// --- WinFeedback: WinFeedbackUI + ScriptedKazancUcusu (bahis katı eşikleri) ---
export const WIN_FEEDBACK = {
  esikler: { big: 2, mega: 5, epic: 15 },
  metinler: { big: "BÜYÜK KAZANÇ", mega: "MUHTEŞEM KAZANÇ", epic: "EFSANE KAZANÇ" },
  girisSn: 0.30, cikisSn: 0.25, saymaFazlaSn: 0.5, kazancUcusSn: 1.95,
};

// --- Ekonomi: ScriptedFinalEkrani + envanter ---
export const EKONOMI = { baslangicBakiye: 50000, borcMiktari: 50000, bonusTuzagiOdeme: 4000 };

// --- Kayıt anahtarları: SaveLoadServisi + GameManager (şema birebir) ---
export const KAYIT = { anaAnahtar: "KumarSaveData_v1", saveSurumu: 1 };

// --- Spin tipleri: ScriptedSpinKaydi enum ---
export const SPIN_TIPI = { SIFIR: 0, NEAR_MISS: 1, KAZANC: 2, MEGA_WIN: 3, BONUS_TETIK: 4, BAHIS_IADESI: 5 };
