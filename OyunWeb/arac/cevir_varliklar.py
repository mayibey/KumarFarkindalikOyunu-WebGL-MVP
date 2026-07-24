# -*- coding: utf-8 -*-
# Unity varliklarini web'e tasir: PNG->WebP (q85), Turkce adlar ASCII'ye normallesir
# (URL guvenligi), sesler/fontlar kopyalanir. Eslemeler ACIK tablo — birebirlik denetlenebilir.
import os, shutil, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
from PIL import Image

G = r"D:\KumarFarkindalikOyunu\Assets\Gorseller"
R = r"D:\KumarFarkindalikOyunu\Assets\Resources"
A = r"D:\KumarFarkindalikOyunu\Assets"
H = r"D:\KumarFarkindalikOyunu\OyunWeb\varlik"

# (kaynak, hedef ascii ad) — sembol id eslemesi sabitler.js'te yapilacak
GORSELLER = [
    (G + r"\armut.png", "sembol_armut"),
    (G + r"\elmalar.png", "sembol_elma"),
    (G + r"\errriklerrrr.png", "sembol_erik"),
    (G + r"\hindistancevizi.png", "sembol_hindistancevizi"),
    (G + r"\karpuz.png", "sembol_karpuz"),
    (G + r"\muz.png", "sembol_muz"),
    (G + r"\üzzzümmmm.png", "sembol_uzum"),
    (G + r"\çilleekkk.png", "sembol_cilek"),
    (G + r"\yıldız.png", "sembol_yildiz"),
    (G + r"\bonanzabomba.png", "sembol_bomba"),
    (G + r"\yeni görseller\son\bg test 1.png", "arkaplan_oyun"),
    (G + r"\yeni görseller\son\11 oyuntahtasıpng.png", "oyun_tahtasi"),
    (G + r"\yeni görseller\son\10 meyvehucre.png", "meyve_hucre"),
    (G + r"\yeni görseller\son\9 spin png.png", "btn_spin"),
    (G + r"\yeni görseller\son\6 bahis azalt png.png", "btn_bahis_azalt"),
    (G + r"\yeni görseller\son\7 bahisarttırpng.png", "btn_bahis_artir"),
    (G + r"\yeni görseller\son\ayarlar png.png", "btn_ayarlar"),
    (G + r"\yeni görseller\son\2 kazanç.png", "etiket_kazanc"),
    (G + r"\yeni görseller\son\kazanc.png", "etiket_kazanc2"),
    (G + r"\yeni görseller\son\kayıp.png", "etiket_kayip"),
    (G + r"\yeni görseller\son\kayipbtn.png", "btn_kayip"),
    (G + r"\yeni görseller\son\3 2x şans.png", "etiket_2x_sans"),
    (G + r"\yeni görseller\son\1 son oyunlar png.png", "etiket_son_oyunlar"),
    (G + r"\yeni görseller\son\14 kampış14.png", "gorsel_kampis"),
    (G + r"\yeni görseller\son\kumaryazısı küçük  logo.png", "logo_kumar_yazisi"),
    (G + r"\siirt bonanza gerçek logo.png", "logo_siirt_bonanza"),
    (G + r"\yeni görseller\oyuna başla buton.png", "btn_oyuna_basla"),
    (G + r"\yeni görseller\buton bos plaka.png", "btn_bos_plaka"),
    (G + r"\spin.png", "btn_spin_eski"),
    (G + r"\otomatik spn 1.png", "btn_otospin"),
    (G + r"\cekbuton.png", "btn_cek"),
    (G + r"\kirmizi_buton.png", "btn_kirmizi"),
    (G + r"\yesil_buton.png", "btn_yesil"),
    (G + r"\bonus_satin_al.png", "btn_bonus_satin_al"),
    (G + r"\satınalbtn.png", "btn_satin_al"),
    (G + r"\bakiye_yukle.png", "btn_bakiye_yukle"),
    (G + r"\yukle_buton.png", "btn_yukle"),
    (G + r"\para_cek.png", "btn_para_cek"),
    (G + r"\eksiltme butobu.png", "btn_eksilt"),
    (G + r"\geridon.png", "btn_geri_don"),
    (G + r"\geri.png", "btn_geri"),
    (G + r"\vazgecbtn.png", "btn_vazgec"),
    (G + r"\devametbtn.png", "btn_devam_et"),
    (G + r"\devamet.png", "btn_devam"),
    (G + r"\sıfırlabtn.png", "btn_sifirla"),
    (G + r"\sıfırlabtn2.png", "btn_sifirla2"),
    (G + r"\kapat_buton.png", "btn_kapat"),
    (G + r"\yonetici.png", "btn_yonetici"),
    (G + r"\istatistikler.png", "btn_istatistikler"),
    (G + r"\bahistxt.png", "etiket_bahis"),
    (G + r"\bkiyetxt.png", "etiket_bakiye"),
    (G + r"\kazanclabel.png", "etiket_kazanc_label"),
    (G + r"\bonusoyun.png", "etiket_bonus_oyun"),
    (G + r"\bonuskazandınız.png", "etiket_bonus_kazandiniz"),
    (G + r"\ödülhavuzu.png", "etiket_odul_havuzu"),
    (G + r"\kasa.png", "etiket_kasa"),
    (G + r"\yeni görseller\ADMİNPANEL\bg.jpg", "admin_bg"),
    (G + r"\yeni görseller\ADMİNPANEL\egilimslider.png", "admin_egilim_slider"),
    (G + r"\yeni görseller\ADMİNPANEL\handle.png", "admin_handle"),
    (G + r"\yeni görseller\ADMİNPANEL\uygula.png", "admin_uygula"),
    (R + r"\egitmenyuz.png", "egitmen_yuz"),
    (R + r"\yuzkafa.png", "egitmen_kafa"),
    (A + r"\panelarkaplan.png", "panel_arkaplan"),
]

SESLER = [
    (A + r"\hareketli fon müzikleri.mp3", "fon_muzigi.mp3"),
    (A + r"\Ses\Alkış sesi.mp3", "alkis.mp3"),
    (A + r"\Ses\Dıkş Sesi.mp3", "sayac_tik.mp3"),
    (A + r"\Ses\Bone Break Crack Snap Sound Effect (original).mp3", "tumble_pop.mp3"),
    (A + r"\Ses\The Price is Right Losing Horn - Sound Effect (HD).mp3", "kayip_horn.mp3"),
    (A + r"\Ses\Şimşek sesi.mp3", "simsek.mp3"),
    (A + r"\Ses\lordsonny-thunder-for-anime-161022.mp3", "gok_gurultusu.mp3"),
    (A + r"\Ses\freesound_community-synth-bass-drop-impact-14706.mp3", "bas_dusme.mp3"),
    (A + r"\Ses\Sith Lightsaber Sound FX.mp3", "isin_kilici.mp3"),
]

FONTLAR = [
    (A + r"\fonts\LilitaOne-Regular.ttf", "LilitaOne-Regular.ttf"),
    (R + r"\Fonts\NotoSansLira\NotoSans-Variable.ttf", "NotoSans-Variable.ttf"),
]

for alt in ("gorsel", "ses", "font"):
    os.makedirs(os.path.join(H, alt), exist_ok=True)

eksik, toplam_once, toplam_sonra = [], 0, 0
for kaynak, ad in GORSELLER:
    if not os.path.exists(kaynak):
        eksik.append(kaynak); continue
    toplam_once += os.path.getsize(kaynak)
    im = Image.open(kaynak)
    hedef = os.path.join(H, "gorsel", ad + ".webp")
    im.save(hedef, "WEBP", quality=85)
    toplam_sonra += os.path.getsize(hedef)

ses_eksik = []
for kaynak, ad in SESLER:
    if not os.path.exists(kaynak):
        ses_eksik.append(kaynak); continue
    shutil.copy2(kaynak, os.path.join(H, "ses", ad))

font_eksik = []
for kaynak, ad in FONTLAR:
    if not os.path.exists(kaynak):
        font_eksik.append(kaynak); continue
    shutil.copy2(kaynak, os.path.join(H, "font", ad))

print(f"gorsel: {len(GORSELLER)-len(eksik)}/{len(GORSELLER)} cevrildi, {toplam_once/1e6:.1f} MB -> {toplam_sonra/1e6:.1f} MB")
print(f"ses: {len(SESLER)-len(ses_eksik)}/{len(SESLER)}, font: {len(FONTLAR)-len(font_eksik)}/{len(FONTLAR)}")
for e in eksik + ses_eksik + font_eksik:
    print("EKSIK:", e)
