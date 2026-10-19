# Final Video Montaj Planı (kullanıcı brief'i 2026-07-26)

## Şablon (onaylı)
Her oyunlu aşama: SLAYT + anlatım sesi → köprü ("Şimdi bunun oyundaki karşılığına bakalım", kopru_oyun.mp3) çalarken CANLI DRAG geçişi → OYUN + oyun müziği (kayıt sesi kısık/ducking).
Slaytlar donuk kare + anlatım süresine uzatılır. Panel (perde arkası) YOK.

## Kalite / Kayıt
- kayit.py: gdigrab desktop 1920x1200 + Stereo Karışımı, libx264 veryfast crf14, aac 256k. ~0.97x realtime.
- dur.flag ile durur (q → finalize). Kayıt: scratchpad/kayit.mp4

## Geçiş noktaları (kullanıcı brief)
- **Aşama 2 (Isındırma):** slayt→oyun geçişi ÜSTTEKİ YAZIDAN yapılıyor. Giriş/çıkış oraya göre kesilecek.
- **Aşama 5 (Şansım Döndü):** geçiş SAĞ ALTTAKİ oyun sembolü butonundan.

## Aşama 5 özel kurgu
1. Kullanıcı önce oyunu açıp 4 spin atıyor (100x çarpan 5. spinde düşüyor — hazırlık).
2. Slaytı açıp oyunu arka planda bekletiyor.
3. Slayttan sağ alt oyun sembolünden oyuna iniyor, ses aç + ekran büyüt.
4. 5. spin → 100x düşer → büyük kazanç yazısı.
5. Büyük kazanç sonrası oyunun açıklama modalı gelir ("oyuncu çıkmayı düşünebilir" içerikli).
6. TAM O ANDA: 13_asama5_sansim.mp3 (çevrim şartı / para çekme tuzağı, ~40s) devreye girer; ALTINDA oyun sesi kısık.
   -> Yani Aşama 5'te anlatım oyun görüntüsü üzerine biner (modal+büyük kazanç anında), slayt+anlatım-önce-oyun şablonundan farklı.

## Aşama 6 (Sonunu Düşünen Kahraman Olamaz) — kullanıcı notu
Oyunlu. Kullanıcı önce 3-4 spin attı, sonra slayta gelip tekrar oyuna döndü (Aşama 5 mantığıyla kurgula/kes).

## KAYIT
scratchpad/kayit.mp4 = 399.7 sn (6.7 dk), 152 MB, crf14. Bu FINAL kayıt. Panele kadar (giriş 01-08 + aşama 09-16) montajlanacak. Panel+kapanış (17-22) YOK.

## Sesler (D:\KumarFarkindalikOyunu\ses_uretim\video\ses dosyaları\)
- 22 sahne mp3 + kopru_oyun.mp3. Aşama sesleri: 09..16. Aşama5=13_asama5_sansim (çevrim şartı eklendi, 40s).
- Oyunlu aşamalar: 2,4,5,8. Slayt-only: giriş 1-7, aşama 1/3/6/7, kapanış 19-22. (Aşama 3+6 atlanabilir - süre için, kullanıcı onayı bekliyor.)

## tekses.py: tek dosya TTS yeniden üret. py tekses.py <stem>
