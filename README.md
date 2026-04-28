# KumarFarkindalikOyunu - WebGL MVP

Unity WebGL MVP cikti klasoru: `Builds/WebGL_MVP`

## Lokal Test

1. `Builds/WebGL_MVP` klasorunde statik server ac.
2. Tarayicida `http://localhost:<port>` ac.
3. Su akislari kontrol et:
   - giris -> oyun acilisi
   - spin / tumble / kazanc
   - admin slider degisiklikleri
   - bakiye yukle / para cek / istatistik
   - ses unlock (ilk etkilesim sonrasi)
   - profil kayit/okuma (refresh sonrasi)
   - log export (clipboard)

## Deploy (Vercel)

- Framework: `Other`
- Root/Output: repository root
- `vercel.json` ile `/` istegi `Builds/WebGL_MVP/index.html` dosyasina rewrite edilir.
- `Build`, `TemplateData` ve `index.html` mevcut yapiyla deploy edilir.

## Deploy Sonrasi Hızlı Smoke Test

1. Public URL aciliyor mu?
2. Canvas ve buton inputlari calisiyor mu?
3. Ses ilk etkilesim sonrasi aciliyor mu?
4. Profil kaydi refresh sonrasi duruyor mu?
5. Log export clipboard fallback calisiyor mu?
6. Debug panelde kritik durumlar gorunuyor mu?

## Not

- WebGL runtime hata koprusu `Builds/WebGL_MVP/index.html` icinde korunmustur (`window.onerror` ve `unhandledrejection`).
- Yeni build alinirsa bu degisikligin kaybolmamasi icin WebGL template tarafina da tasinmalidir.
