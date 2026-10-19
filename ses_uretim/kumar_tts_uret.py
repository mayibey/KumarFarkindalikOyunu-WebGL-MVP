# -*- coding: utf-8 -*-
"""
JSPS Fabrika — ElevenLabs batch TTS uretici (ham REST, SDK YOK)
Python 3.14 / Windows. .txt ses metinlerini secilen sesle mp3'e cevirir.

Kullanim:
  py jsps_tts_uret.py --dry-run     -> sadece ozet + uretilecek/atlanacak liste (API yok)
  py jsps_tts_uret.py               -> on-ucus ozeti + onay sorar, sonra uretir
  py jsps_tts_uret.py --yes         -> onayi atla, dogrudan uretir
"""
import sys, os, re, time
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

import requests

# ============================== CONFIG ==============================
KOK            = r"D:\KumarFarkindalikOyunu\ses_uretim"
ENV_PATH       = r"D:\JSPS Fabrika\.env"
VOICE_ID       = "jNEAGPibhhLMspJ6h1ow"
MODEL_ID       = "eleven_flash_v2_5"
OUTPUT_FORMAT  = "mp3_44100_128"
STABILITY      = 0.5
SIMILARITY     = 0.75
SPEAKER_BOOST  = True
DELAY_SN       = 0.3
BEKLENEN_ADET  = 5

API_URL        = f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}?output_format={OUTPUT_FORMAT}"
HAM_RAKAM_LOG  = Path(KOK) / "_TTS_HAM_RAKAM_INCELE.txt"

KREDI_KARAKTER_KATSAYI = 0.5          # Flash: 1 karakter = 0.5 kredi
USD_PER_1000_KARAKTER  = 0.05         # tahmini maliyet
MIN_MP3_BAYT           = 1024         # >1KB ise gecerli sayilir (resume/skip)

# ============================== YARDIMCI ==============================
def api_anahtari_oku():
    """Once .env'i manuel parse et (KEY=VALUE, # yorum atla); sonra ortam degiskeni."""
    p = Path(ENV_PATH)
    if p.exists():
        for satir in p.read_text(encoding="utf-8-sig").splitlines():
            s = satir.strip()
            if not s or s.startswith("#") or "=" not in s:
                continue
            k, _, v = s.partition("=")
            if k.strip() == "ELEVENLABS_API_KEY":
                v = v.strip().strip('"').strip("'")
                if v:
                    return v
    return os.environ.get("ELEVENLABS_API_KEY", "").strip()


def ham_rakam_var(metin):
    """Flash normalizasyon tuzagi: metinde ham [0-9] rakami var mi?"""
    return re.search(r"[0-9]", metin) is not None


def insanca(n):
    return f"{n:,}".replace(",", ".")


def kanun_argini_oku():
    """--kanun DEGER  veya  --kanun=DEGER  -> deger.
    Verilmediyse None; verildi ama deger yoksa "" (hata olarak ele alinir)."""
    argv = sys.argv
    for i, a in enumerate(argv):
        if a == "--kanun":
            if i + 1 < len(argv) and not argv[i + 1].startswith("--"):
                return argv[i + 1].strip()
            return ""
        if a.startswith("--kanun="):
            return a.split("=", 1)[1].strip()
    return None


# ============================== ANA ==============================
def main():
    dry_run = "--dry-run" in sys.argv
    auto_yes = "--yes" in sys.argv

    api_key = api_anahtari_oku()
    if not api_key:
        if dry_run:
            print("UYARI: API anahtari bulunamadi (.env / ELEVENLABS_API_KEY). Dry-run devam ediyor.")
        else:
            print("HATA: ELEVENLABS_API_KEY bulunamadi. .env dosyasini ya da ortam degiskenini ayarla.")
            sys.exit(1)

    kok = Path(KOK)
    if not kok.exists():
        print(f"HATA: KOK bulunamadi: {KOK}")
        sys.exit(1)

    # --- kanun klasorlerini belirle (--kanun filtresi) ---
    tum_kanunlar = sorted([d for d in kok.iterdir()
                           if d.is_dir() and (d / "ses_metinleri").is_dir()])
    kanun_arg = kanun_argini_oku()
    if kanun_arg is None:
        secili_kanunlar = tum_kanunlar
        secim_aciklama = "TUM kanunlar"
    else:
        if not kanun_arg:
            print("HATA: --kanun bir deger gerektirir. Ornek: py jsps_tts_uret.py --kanun 5237TCK")
            sys.exit(1)
        # klasor adinda gecen metne gore, case-insensitive 'in' mantigi
        secili_kanunlar = [d for d in tum_kanunlar if kanun_arg.lower() in d.name.lower()]
        if len(secili_kanunlar) == 0:
            print(f"HATA: '--kanun {kanun_arg}' ile eslesen klasor yok. Mevcut kanun klasorleri:")
            for d in tum_kanunlar:
                print(f"   - {d.name}")
            sys.exit(1)
        if len(secili_kanunlar) > 1:
            print(f"UYARI: '--kanun {kanun_arg}' BIRDEN FAZLA klasore esledi ({len(secili_kanunlar)}). "
                  f"Yanlislikla fazlasini uretmemek icin daha SPESIFIK ol:")
            for d in secili_kanunlar:
                print(f"   - {d.name}")
            sys.exit(1)
        secim_aciklama = f"SADECE: {secili_kanunlar[0].name}"

    # --- girdileri topla ---
    txtler = []
    for d in secili_kanunlar:
        txtler.extend((d / "ses_metinleri").glob("*.txt"))
    txtler = sorted(txtler)
    print(f"Secim: {secim_aciklama}  ({len(secili_kanunlar)} klasor)")
    if kanun_arg is None:
        print(f"Bulunan .txt: {len(txtler)} (beklenen {BEKLENEN_ADET})")
        if len(txtler) != BEKLENEN_ADET:
            print(f"  UYARI: beklenen {BEKLENEN_ADET} degil ({len(txtler)}). Yine de devam.")
    else:
        print(f"Bulunan .txt: {len(txtler)}")

    uretilecek = []   # (txt_path, mp3_path, metin, char, ham_rakam)
    atlanan = 0
    bos = 0
    ham_rakam_dosyalar = []

    for txt in txtler:
        try:
            metin = txt.read_text(encoding="utf-8-sig").strip()
        except Exception as e:
            print(f"  OKUNAMADI atlandi: {txt.name} ({e})")
            continue
        if not metin:
            bos += 1
            print(f"  BOS atlandi: {txt.name}")
            continue

        kanun_dir = txt.parent.parent                      # .../{kanun}
        mp3 = kanun_dir / "ses dosyaları" / (txt.stem + ".mp3")

        hr = ham_rakam_var(metin)
        if hr:
            ham_rakam_dosyalar.append(str(txt))

        if mp3.exists() and mp3.stat().st_size > MIN_MP3_BAYT:
            atlanan += 1
            continue

        uretilecek.append((txt, mp3, metin, len(metin), hr))

    # --- ham rakam inceleme dosyasi (her zaman yazilir; tani amacli) ---
    try:
        HAM_RAKAM_LOG.write_text(
            "# Ham rakam (0-9) iceren ses metinleri — Flash v2.5 normalizasyon tuzagi icin gozden gecir\n"
            + ("\n".join(ham_rakam_dosyalar) if ham_rakam_dosyalar else "(yok)") + "\n",
            encoding="utf-8")
    except Exception as e:
        print(f"  (ham rakam log yazilamadi: {e})")

    toplam_char = sum(x[3] for x in uretilecek)
    tahmini_kredi = int(round(toplam_char * KREDI_KARAKTER_KATSAYI))
    tahmini_usd = toplam_char / 1000.0 * USD_PER_1000_KARAKTER

    # --- on-ucus ozeti ---
    print("\n================ ON-UCUS OZETI ================")
    print(f"  Secim                   : {secim_aciklama}")
    if kanun_arg is not None:
        print(f"  Klasor                  : {secili_kanunlar[0].name}")
    print(f"  Uretilecek (atlanmayan) : {len(uretilecek)}")
    print(f"  Atlanan (mevcut mp3)    : {atlanan}")
    print(f"  Bos atlanan             : {bos}")
    print(f"  Toplam karakter         : {insanca(toplam_char)}")
    print(f"  Tahmini Flash kredi     : {insanca(tahmini_kredi)}  (karakter x {KREDI_KARAKTER_KATSAYI})")
    print(f"  Tahmini maliyet         : ${tahmini_usd:,.2f}  (karakter/1000 x ${USD_PER_1000_KARAKTER})")
    print(f"  Ham rakamli dosya       : {len(ham_rakam_dosyalar)}  -> {HAM_RAKAM_LOG}")
    print(f"  Ses     : voice={VOICE_ID}  model={MODEL_ID}  format={OUTPUT_FORMAT}")
    print(f"  Ayarlar : stability={STABILITY} similarity={SIMILARITY} speaker_boost={SPEAKER_BOOST}")
    print("===============================================")

    # --- dry-run: liste yazdir, API YOK ---
    if dry_run:
        print("\n[DRY-RUN] Uretilecek dosyalar:")
        for txt, mp3, metin, c, hr in uretilecek:
            isaret = " ⚠ ham rakam" if hr else ""
            print(f"   + {txt.parent.parent.name} / {txt.stem}  ({c} krk){isaret}")
        if atlanan:
            print(f"\n[DRY-RUN] {atlanan} dosya zaten mp3'lu (atlanacak).")
        print("\n[DRY-RUN] API cagrisi YAPILMADI. Gercek uretim icin --dry-run'siz calistir.")
        return

    if not uretilecek:
        print("\nUretilecek dosya yok (hepsi mevcut). Cikiliyor.")
        return

    # --- onay ---
    if not auto_yes:
        cevap = input("\nDevam edilsin mi? (e/h): ").strip().lower()
        if cevap != "e":
            print("Iptal edildi.")
            return

    # ============================== URETIM ==============================
    uretilen = atlanan_uretim = hatali = 0
    harcanan_char = 0
    hata_listesi = []
    toplam = len(uretilecek)
    headers = {"xi-api-key": api_key, "Content-Type": "application/json"}

    for i, (txt, mp3, metin, char, hr) in enumerate(uretilecek, 1):
        etiket = f"[{i}/{toplam}] {txt.parent.parent.name}/{txt.stem}"
        hr_isaret = " ⚠ham" if hr else ""
        body = {
            "text": metin,
            "model_id": MODEL_ID,
            "voice_settings": {
                "stability": STABILITY,
                "similarity_boost": SIMILARITY,
                "use_speaker_boost": SPEAKER_BOOST,
            },
        }

        basarili = False
        backoff = [2, 5, 10]
        deneme = 0
        while True:
            try:
                r = requests.post(API_URL, headers=headers, json=body, timeout=120)
            except requests.RequestException as e:
                # ag hatasi -> retry mantigina dahil et
                if deneme < len(backoff):
                    bekle = backoff[deneme]; deneme += 1
                    print(f"  {etiket} ⟳ ag hatasi ({e}); {bekle}s sonra tekrar...")
                    time.sleep(bekle); continue
                hatali += 1; hata_listesi.append((txt.stem, "AG", str(e)[:80]))
                print(f"✗ {etiket} — AG HATASI: {str(e)[:80]}")
                break

            if r.status_code == 200:
                try:
                    mp3.parent.mkdir(parents=True, exist_ok=True)
                    mp3.write_bytes(r.content)
                    uretilen += 1; harcanan_char += char; basarili = True
                    print(f"✓ {etiket}  ({len(r.content)//1024} KB){hr_isaret}")
                except Exception as e:
                    hatali += 1; hata_listesi.append((txt.stem, "YAZMA", str(e)[:80]))
                    print(f"✗ {etiket} — YAZILAMADI: {str(e)[:80]}")
                break

            if r.status_code == 429 or 500 <= r.status_code < 600:
                if deneme < len(backoff):
                    bekle = backoff[deneme]; deneme += 1
                    print(f"  {etiket} ⟳ {r.status_code}; {bekle}s sonra tekrar...")
                    time.sleep(bekle); continue
                hatali += 1; hata_listesi.append((txt.stem, r.status_code, r.text[:80]))
                print(f"✗ {etiket} — {r.status_code} (retry bitti): {r.text[:80]}")
                break

            if r.status_code == 401:
                govde = r.text.lower()
                if "quota" in govde or "credit" in govde:
                    print(f"\n⛔ KOTA BITTI ({r.status_code}): {r.text[:120]}")
                    print(f"   O ana kadar: uretilen={uretilen}, atlanan(onceden)={atlanan}, hatali={hatali}")
                    print(f"   Harcanan tahmini kredi: {insanca(int(round(harcanan_char*KREDI_KARAKTER_KATSAYI)))}")
                    print("   -> Top-up yapip tekrar calistir; resume devreye girer (mevcut mp3'ler atlanir).")
                    _final_rapor(uretilen, atlanan, hatali, harcanan_char, hata_listesi, kismi=True)
                    return
                else:
                    print(f"\n⛔ YETKI HATASI (401): API anahtari gecersiz olabilir. {r.text[:120]}")
                    sys.exit(1)

            if r.status_code == 422:
                hatali += 1; hata_listesi.append((txt.stem, 422, r.text[:80]))
                print(f"✗ {etiket} — 422 (gecersiz icerik), atlandi: {r.text[:80]}")
                break

            # diger beklenmedik kodlar
            hatali += 1; hata_listesi.append((txt.stem, r.status_code, r.text[:80]))
            print(f"✗ {etiket} — beklenmedik {r.status_code}: {r.text[:80]}")
            break

        if basarili and DELAY_SN:
            time.sleep(DELAY_SN)

    _final_rapor(uretilen, atlanan, hatali, harcanan_char, hata_listesi, kismi=False)


def _final_rapor(uretilen, atlanan, hatali, harcanan_char, hata_listesi, kismi):
    print("\n================ FINAL RAPOR ================")
    print(f"  {'KISMI (kota)' if kismi else 'TAMAMLANDI'}")
    print(f"  Uretilen : {uretilen}")
    print(f"  Atlanan  : {atlanan} (zaten mp3'lu)")
    print(f"  Hatali   : {hatali}")
    print(f"  Harcanan tahmini kredi: {insanca(int(round(harcanan_char*KREDI_KARAKTER_KATSAYI)))}  ({insanca(harcanan_char)} karakter)")
    if hata_listesi:
        print("  --- HATALI DOSYALAR ---")
        for ad, kod, mesaj in hata_listesi:
            print(f"     ✗ {ad} [{kod}] {mesaj}")
    print(f"  Ham rakam inceleme: {HAM_RAKAM_LOG}")
    print("============================================")


if __name__ == "__main__":
    main()

