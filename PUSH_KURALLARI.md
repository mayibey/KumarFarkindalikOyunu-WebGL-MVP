# PUSH VE BRANCH KURALLARI

## Tek branch: master
- Geliştirme master branch'inde yapılır.
- master = hem kaynak kod hem WebGL build. Tek branch.
- refactor-paket8 EMEKLİYE AYRILDI (21 May 2026): orphan history, terk
  edilmiş, sadece ölü yedek. DOKUNMA, fast-forward/merge/cherry-pick DENEME
  (orphan olduğu için çalışmaz, kriz çıkarır).

## Push nasıl yapılır
1. git push origin master
2. Vercel master'dan otomatik deploy eder (~2-5 dk):
   https://kumar-farkindalik-oyunu-web-gl-mvp.vercel.app
3. Build artifact'leri (Builds/WebGL_MVP/) commit'e dahil edilir — master
   hem kaynak hem deploy branch'i.

## LFS TUZAĞI (kritik)
- .data ve .wasm dosyaları Git LFS ile tutulur.
- Eğer bunlar 136-byte pointer olarak görünüyorsa: git lfs pull ile resolve
  et. Pointer halde push edilirse Vercel'de oyun BOZUK yüklenir.
- WebGL_MVP.wasm ~70MB → GitHub'ın 50MB uyarısı NORMAL, LFS'ten geliyor.

## Yedek branch'ler (geri alma noktaları, silme)
- master-yedek-35-32-38
- backup-sahne-silme-oncesi

## Commit kuralları
- Mesaj Türkçe, "Faz 35.X" formatı, Co-Authored-By satırı dahil.
- Push patron onayı olmadan ASLA yapılmaz.
