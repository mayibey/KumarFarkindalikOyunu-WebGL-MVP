# PIXI_PLAN — "Kumar Kazandırmaz" PixiJS Taşıma Planı (2026-07-24)

Amaç: Unity WebGL oyununun **fark edilemeyecek kadar birebir**, telefonda sunumla aynı
sekmede çökmeden çalışan PixiJS sürümü. Kaynak envanter: keşif raporu (bu dosyanın temeli)
+ kökteki `SCRIPTED_SPIN_BILGI.md`, `KRITIK_AYARLAR_VE_SPIN_AKISI_ANALIZI.md`,
`02_SenaryoluOyun_TUM_Metinler.md`.

## 0) Mimari Kararlar (KESİN)

| Karar | Gerekçe |
|---|---|
| PixiJS v8, `vendor/pixi.min.js` YEREL kopya | CDN yasak — sunum paketi internetsiz çalışır |
| Bundler YOK; native ES modules | Build adımı yok → dağıtım = klasör kopyala; Claude'suz da bakılabilir |
| Kaynak `OyunWeb/` = dağıtılabilir klasör; canlıda `Builds/WebGL_MVP/oyun2/` | Unity sürümüne DOKUNULMAZ; paralel test, onayla geçiş, anında geri dönüş |
| `window.unityInstance.SendMessage` shim | sunum/panel/anlatıcı köprüleri TEK SATIR değişmeden çalışır |
| anlatici.html / panel.html / bahisSec.html AYNEN iframe | Zaten web; postMessage protokolleri korunur (aşağıda spec) |
| Senaryo verisi `ScriptedSenaryo.asset` → `veri/senaryo.json` script'le üretilir | Elle yazım yok = birebirlik garantisi |
| Sabitler `veri/sabitler.js` tek dosyada, Unity kaynağı satır referanslı | Denetlenebilirlik |
| Coroutine akışları → async/await + `bekle(ms)`/`bekleKosul(fn)` yardımcıları | Unity WaitForSeconds/WaitUntil birebir karşılığı |
| Kayıt: localStorage, Unity anahtar ŞEMASI korunur (`KumarSaveData_v1`, `PP_*`) | Davranış eşitliği |
| 1920×1080 sabit sahne + letterbox (OLCEK_WRAPPER deseni) + OYUN_ZOOM + PINCH_KILIT | Kanıtlanmış mobil düzen |

## 1) Kapsam

**VAR:** 01 Giriş (canlı slot demo arka planı + isim modalı + 2 buton), 02 Senaryolu Oyun
(7 aşama, anlatıcı şerit, scripted spinler + dinamik eğilim aşamaları, bonus tuzağı,
düşünce balonu, borç paneli, final ekranı), 03 Admin/Manipülasyon (panel.html, 6 mod,
tüm ayar anahtarları), WinFeedback (BIG/MEGA/EPIC), bahisSec modalı, ses seti, kayıt/devam.

**YOK (bilinçli):** 04 Tutorial sahnesi + Tutorial/ 18 script (kullanılmıyor),
05_LogScane, Editor araçları, AdminGirisDogrulama şifresi (şifresiz geçiş zaten istenmişti).

## 2) Modül Haritası (OyunWeb/)

```
OyunWeb/
  index.html            # letterbox sarmalayıcı (PINCH_KILIT + OYUN_ZOOM + ?debug eruda)
  oyun.html             # 1920x1080 sahne; sunumun iframe'lediği sayfa
  vendor/pixi.min.js
  veri/senaryo.json     # .asset'ten üretilir (uret_senaryo_json.py)
  veri/sabitler.js      # paytable, mod presetleri, aşama ayarları, eşikler (satır ref'li)
  veri/metinler.js      # modal metinleri (TMP rich text → span çevirisi)
  varlik/{gorsel,ses,font}/
  js/
    cekirdek/rng.js sahne.js yukleyici.js zamanlayici.js kayit.js ses.js
    motor/izgara.js cluster.js tumble.js carpan.js odeme.js egilim.js
          spinMotoru.js senaryoMotoru.js scriptedOynatici.js motorlar/(hook|yontma|tutma|koruma|nearmiss|ozel).js
    ui/girisEkrani.js slotGorunum.js altSerit.js winFeedback.js modallar.js
       egitmenModal.js dusunceBalonu.js bonusTuzagi.js yuklemePaneli.js finalEkrani.js
       bonusHUD.js havaiFisek.js
    kopru/sendMessageShim.js panelKopru.js anlaticiKopru.js bahisKopru.js sunumKoprusu.js
    sahneler/giris.js senaryolu.js admin.js
  test/motor.test.mjs senaryo.test.mjs   # node --test; tarayıcısız
```

## 3) Birebir Taşınacak Sayısal Çekirdek (Unity kaynağı → sabitler.js)

- Cluster: grid genelinde aynı sembol ≥ **8** (4-yön komşuluk YOK; scatter=7 hariç)
  — `TumbleServisi.FindClustersToRemove:36`.
- Tumble: yerinde patla→aynı hücreye düş (yerçekimi kayması YOK) — `CokmeAkisServisi`.
- Paytable: `TumbleAyarlari.cs` (451) değerleri AYNEN. Fallback `count×10×bahis`.
- Çarpan: `CARPAN_SEMBOL=-2`, SUM toplama, üretim %2, `MAX_CARPAN_TAVAN=5`, force x5-x100.
- Eğilim/reroll: `OdemeModelineUygunMu` (Admin.cs:185) — yön zorlaması + reroll
  (post-clamp YOK); bant `[bahis×min, bahis×maks]` sadece senaryo modlarında.
  Reroll bütçesi `AsamaIcinMaxReroll` 20-2000, `SIMULASYON_MAX_REROLL=28`.
- Mod presetleri (PanelKopru:490-559): normal 65/0/0 · hook 90/1.1/2.2 · yontma 70/0.3/0.7
  · tutma 15/1.1/1.5(+2kayıp-1kazanç döngüsü) · koruma 8/0.1/0.3 · ozel 65/manuel.
- Anlatıcı aşama tablosu (AnlaticiSeritKopru): eğilim %95→%5, maxÇarpan 5.0→0.1,
  bahisler {500,1500,1500,2500,4000,10000,1500}, spin hedefleri {8,8,8,5,4,5,999}.
- WinFeedback eşikleri: ≥2×bahis BIG · (5× MEGA · 15× EPIC — WinFeedbackUI'den teyit edilecek);
  süreler 0.30/0.25/0.5s; metinler BÜYÜK/MUHTEŞEM/EFSANE KAZANÇ.
- Ekonomi: başlangıç 50.000, borç +50.000, bonus tuzağı toplam 4.000 ödeme (10 sabit spin).
- Save şeması: `KumarSaveData_v1` alanları AYNEN (saveSurumu…adminMaxOdeme).

## 4) Köprü Protokol Spec'i (değişmez sözleşme)

- panel.html → oyun: postMessage `{source:'yoneticiPanel', key, value}`; key seti:
  oyunModu, minCarpan, maksCarpan, yakinKacirma, carpanOlasilik, maxCarpanTekSpin,
  carpanZorla, carpanOdeme, carpanTumble, bonus*, scatterBaskila, detayliAyarlarAcik,
  varsayilanaDon, uygulamaOnayi, tumAyarlar, panelHazir, paneliKapat.
- oyun → panel: `{source:'unityToPanel', mevcutAyarlar|zorlaTuketildi|bahisBakiye}`.
- anlatici: oyun→ `{source:'unityToAnlatici', asama,spin,hedefSpin,bakiyeNet,toplamSpin,
  spinNetleri[],tukenis,tukenisKapat}`; anlatıcı→ `anlaticiAsamaDegis`, `anlaticiYenidenBaslat`,
  `ready`, `hoverZoom` (konteyner 460↔1300px).
- bahisSec: `bahisSec`, `bahisPaneliKapat`; oyun→ `{source:'unityToBahis', bakiye}`.
- Dış dünya shim'i: `window.unityInstance.SendMessage(obj, metod, arg)` →
  SunumKoprusu.{SunumAsamaGit,SunumPanelGit,SunumSesAyarla}, PanelKopru.AyarAl,
  AnlaticiSeritKopru.BonusBitisOnayla. `__sesBaglamlari` yakalayıcı da korunur.

## 5) Riskler ve Önlemler

1. Coroutine→async çevirisi (en büyük kalem): `zamanlayici.js` (bekle/bekleKosul/frameBekle)
   ile desen bire bir; her akış fonksiyonu Unity coroutine'inin adını taşır (izlenebilirlik).
2. Reroll performansı: JS'te senkron 2000 iterasyon < 5 ms — Unity'nin precompute
   karmaşası GEREKMEZ (basitleşme); yine de spin butonunda ölçüm log'u.
3. Animasyon hissi: her animasyonun süre/easing'i Unity kodundan sayıyla alınacak
   (ör. kazanç uçuşu 1.95s SmoothStep); yan yana video karşılaştırması faz 7'de.
4. TMP rich text: `<color=#..><b>` → `<span>` mini-parser (`metinler.js` üretiminde).
5. Particle (PopParticle) + döner ışınlar + havai fişek: Pixi Ticker + ADD blend; jslib'deki
   canvas havai fişek kodu neredeyse aynen taşınır.
6. Ses: WebAudio; klip-içi aralık çalma (`src.time` karşılığı offset param), pitch 0.96-1.06;
   iOS ilk dokunuş kilidi; master gain = AudioListener.volume karşılığı.
7. Font: LilitaOne-Regular.ttf + NotoSans-Variable.ttf @font-face; ₺ testi.
8. Giriş "videosu" aslında canlı demo (`GirisDemoAnimator`, tumbleAraligi=4s) — Pixi'de
   aynı seed mantığıyla yeniden (video dosyası yok, risk düştü).
9. buildIndex kayması tuzağı: sahne adlarıyla çalışılacak; kod yorumlarındaki 03/04'e güvenme.
10. Band override üçlemesi: panel.html'de SENARYOLAR 2 kopya — Pixi'de tek kaynak
    `sabitler.js`; panel.html'e dokunulmadığı için oradaki kopyalar aynen kalır.

## 6) Test Düzeneği (otomatik, kullanıcısız)

- `node --test test/*.mjs`: cluster/ödeme/çarpan birim testleri; eğilim istatistiği
  (10k spin × mod → ödeme oranı beklenen aralıkta); scripted senaryo playback snapshot
  (senaryo.json'daki her spin → beklenen net sonuç); save round-trip.
- Playwright: her fazda ekran görüntüsü seti (giriş, spin ortası, kazanç, aşama modalları)
  → Unity karşılığıyla yan yana kompozit; iPhone emülasyonu (390×844 DPR3) akıcılık + bellek.
- Köprü smoke: sunum ?oyun2 ile aşama butonu → SendMessage shim → aşama değişimi assert.

## 7) Fazlar / Kabul Kriterleri

| Faz | Çıktı | Kabul |
|---|---|---|
| F1 iskelet | /oyun2 canlı boş sahne + letterbox + shim + ?debug | telefonda açılır, 60fps |
| F2 varlık | senaryo.json + sabitler.js + atlaslar | json alan sayısı = asset alan sayısı |
| F3 motor | spin/tumble/çarpan/eğilim + testler | tüm birim testler yeşil; 10k spin istatistiği |
| F4 UI | giriş+slot görünüm+alt şerit+winfeedback | görsel karşılaştırma < eşik |
| F5 senaryo | 7 aşama uçtan uca + anlatıcı + scripted overlay'ler | senaryo snapshot testleri |
| F6 admin | panel.html tam entegre, 6 mod | mod istatistik testleri |
| F7 geçiş | sunum /oyun2'ye bağlanır; mobil GERÇEK cihaz onayı (kullanıcı) | patron linki |

Tahmin: F1-F3 ilk yoğun blok; F4-F5 ikinci; F6-F7 üçüncü. Toplam 1,5-2 hafta temposu.
