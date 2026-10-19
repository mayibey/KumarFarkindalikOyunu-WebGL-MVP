# KUMAR FARKINDALIK VİDEOSU — MONTAJ HARİTASI
_(Siirt İl Jandarma personel eğitimi · sanal kumar farkındalık)_
Son güncelleme: 2026-07-26

## KONSEPT
Her aşama şablonu: **temiz SLAYT + anlatım sesi → köprü ("şimdi bunun oyundaki karşılığına bakalım") → xfade slide geçiş → OYUN görüntüsü + oyun müziği.**
Slaytlar sunumdan temiz (oyunsuz) screenshot; oyunlar kullanıcının Unity ekran kaydından. Panel (Hilenin Mutfağı) AYRI çekilecek, sonra eklenecek.
Çıktı: `VIDEO_TAM.mp4` (~7 dk, panele kadar).

## DOSYA KONUMLARI
- **Kalıcı yedek:** `D:\KumarFarkindalikOyunu\video_proje\`
  - `kayit_HAM.mp4` — kullanıcının Unity oyun ekran kaydı (399 sn, 1920x1080, crf14). YENİDEN ÇEKİLEMEZ, en kritik varlık.
  - `slaytlar\s_00..s_18.png` — sunumdan temiz screenshot (1920x1080)
  - `scriptler\` — montaj2.py (master montaj), slayt_cek.py (slayt çek), tekses.py (TTS), kayit.py (ekran kaydı)
- **Sesler:** `D:\KumarFarkindalikOyunu\ses_uretim\video\ses dosyaları\*.mp3`
- **Ses metinleri:** `D:\KumarFarkindalikOyunu\ses_uretim\video\ses_metinleri\*.txt`
- **Slayt kaynağı (sunum):** `D:\KumarFarkindalikOyunu\SunumPaketi\sunum.html` — slaytlar `const slides=[` (satır 519); oyun paneli `body.oyunlu` yoksa gizli (screenshot temiz gelir)
- **Yüzleşme videosu:** `D:\KumarFarkindalikOyunu\SunumPaketi\media\media2.mp4` (17 sn, gelir-gider defteri çöküşü)
- **Çalışma/çıktı (GEÇİCİ, scratchpad):** `...\48ce8bc9-...\scratchpad\` → montaj burada üretir, sonra masaüstüne + video_proje'ye kopyalanır

## SLAYT İNDEKS ↔ SUNUM EŞLEŞMESİ (sunum.html slides[])
| png | slides[] | İçerik |
|---|---|---|
| s_00 | [0] cover | Kapak SANAL KUMAR BAĞIMLILIĞI |
| s_01 | [1] intro | Giriş·01 Sanal Kumar Tuzağı |
| s_02 | [2] intro | Giriş·02 Kolay para |
| s_03 | [3] intro | Giriş·03 Simülasyon tasarladık |
| s_04 | [4] intro | Giriş·04 Hilenin Mutfağı |
| s_05 | [5] intro | Giriş·05 Amaç kaybettirmek |
| s_06 | [6] intro | Giriş·06 Kasa hep kazanır |
| s_07 | [7] divider | 8 AŞAMA |
| s_08 | [8] game | Simülasyonu Canlı İzleyelim (KULLANILMADI) |
| s_09 | [9] stage1 | Bal Tuzağı |
| s_10 | [10] stage2 | Isındırma ve Umut |
| s_11 | [11] stage3 | Kontrol Bende Hissi |
| s_12 | [12] stage4 | Kaybettiklerimi Geri Kazanabilirim |
| s_13 | [13] stage5 | Şansım Döndü |
| s_14 | [14] stage6 | Sonunu Düşünen Kahraman Olamaz |
| s_15 | [15] stage7 | Başka Bir Yerden Para Bulmalıyım |
| s_16 | [16] video | Gerçeklerle Yüzleşme başlık (KULLANILMADI; media2.mp4 kullanıldı) |
| s_17 | [17] stage8 | Tükeniş slaytı (KULLANILMADI; video kullanıldı) |
| s_18 | [18] game panel | Sıra Hilenin Mutfağı'nda (panel-git) |

## VİDEO SAHNE SIRASI (montaj2.py `parts`)
| # | Aşama | Tip | Görüntü | Ses (metin dosyası) | Kayıt sn | Montaj notu |
|---|---|---|---|---|---|---|
| 1 | Kapak | slayt | s_00 | 01_kapak | — | donuk slayt, ses süresi |
| 2 | Tuzak | slayt | s_01 | 02_tuzak | — | |
| 3 | Kolay para | slayt | s_02 | 03_kolaypara | — | |
| 4 | Simülasyon | slayt | s_03 | 04_simulasyon | — | |
| 5 | Hilenin Mutfağı | slayt | s_04 | 05_perdearkasi | — | |
| 6 | Amaç | slayt | s_05 | 06_amac | — | |
| 7 | Kasa | slayt | s_06 | 07_kasa | — | |
| 8 | 8 Aşama | slayt | s_07 | 08_gecis | — | |
| 9 | **A1 Bal Tuzağı** | slayt | s_09 | 09_asama1_baltuzagi | — | oyun yok |
| 10 | **A2 Isındırma** | OYUN | s_10→kayıt | 10_asama2_isindirma + köprü | 106-134 | Büyük Kazanç 1.531 TL |
| 11 | **A3 Kontrol** | slayt | s_11 | 11_asama3_kontrol | — | oyun yok |
| 12 | **A4 Kaybettiklerimi** | OYUN | s_12→kayıt | 12_asama4_kayip + köprü | 186-198 | spin/kayıp, oyun sesi (dadadant) vol=1.8 |
| 13 | **A5 Şansım Döndü** | OYUN ÖZEL | s_13→kayıt | 13a_sansim_tanit (slayt) + 13b_sansim_cevrim (oyun) + köprü | 230-256 | SPIN@232 → 100x düşüş@234 → efsane kazanç@238 → 50.000@241; 13b 13sn gecikmeli (kazanç anına); freeze 16sn; oyun sesi açık; çevrim şartı |
| 14 | **A6 Sonunu Düşünen** | OYUN | s_14→kayıt | 14_asama6_dongu + köprü | 289-305 | 500x→ÇOK ŞANSLISIN→10 SPIN bonus; vol=1.5 |
| 15 | **A7 Başka Bir Yerden** | slayt | s_15 | 15_asama7_kontrolkaybi | — | oyun yok |
| 16 | **A8 Tükeniş** | VİDEO SLAYT | a8_slayt.mp4 | 16_asama8_tukenis | — | Sunum video-slayt düzeni: üstte "Gerçeklerle Yüzleşme" başlığı + altta oynayan media2 video (Playwright kaydı `a8_cek.py`); anlatım üstte |
| 17 | **Panel-git** | slayt | s_18 | 17_perde_gecis | — | "Sıra Hilenin Mutfağı'nda" |

## PATRONA ARZ SONRASI — NEYİ NASIL DEĞİŞTİRİRİM
> Değişiklik türüne göre tek/az adımda aksiyon. Montajı tekrar üretmek: `py scriptler\montaj2.py` (çıktı scratchpad\VIDEO_TAM.mp4).

1. **Bir slaytın GÖRÜNTÜSÜ değişecek** (metin/görsel/renk): `SunumPaketi\sunum.html` içindeki `slides[İNDEKS]` düzenle → `slayt_cek.py` çalıştır (tüm slaytları yeniden çeker, tekilse range daralt) → `montaj2.py`.
2. **Bir aşamanın SESİ/METNİ değişecek:** `ses_metinleri\<dosya>.txt` düzenle → `py scriptler\tekses.py <dosya_adı_uzantısız>` (o mp3'ü yeniden üretir) → `montaj2.py`.
3. **Bir oyun sahnesinin KESİTİ (hangi an) değişecek:** `montaj2.py` içinde o aşamanın `oyun_sahne(..., oyun_ss, oyun_dur, ...)` değerlerini değiştir → çalıştır. (Kayıt referansı: A2=106, A4=186, A5=237, A6=289.)
4. **A5 çevrim şartı sesinin GECİKMESİ:** `montaj2.py` A5 satırı `gecikme=6000` (ms) ve `freeze=16` (sn) ayarla.
5. **Sahne EKLE/ÇIKAR/SIRALA:** `montaj2.py` `parts` bloğunda ilgili satırı ekle/çıkar/taşı.
6. **Oyun sesi seviyesi:** `oyunvol` (normal) / `oyunvol_ozel` (A5) parametreleri.
7. **Panel bölümü:** henüz YOK — ayrı çekilecek, sonra `parts` sonuna eklenecek (17_perde_gecis'ten sonra).

## 3 PARÇA (birleştirilecek)
1. **VIDEO_TAM.mp4** (~7 dk) = giriş + 8 aşama + panel-git slaytı — ✅ HAZIR (montaj2.py)
2. **PANEL** = manipülasyon paneli canlı "kaybettirme" gösterimi — ⏳ kullanıcı çekecek ("ince detay istiyor")
3. **KAPANIS.mp4** (~39 sn) = AMA → Yeni Başlangıç → Cesaret → Kapanış (slayt+ses, `kapanis_montaj.py`, sesler 19_ama/20_yeni/21_cesaret/22_kapanis, slaytlar s_19..s_22) — ✅ HAZIR
→ Panel gelince: 3 parça concat (RE-ENCODE) → tam video ~8-9 dk. Masaüstünde: KumarVideo_TAM.mp4, KumarVideo_KAPANIS.mp4.

## AÇIK İŞLER
- Panel çekimi (kullanıcı) + 3 parça birleştirme (panel gelince, ben).
