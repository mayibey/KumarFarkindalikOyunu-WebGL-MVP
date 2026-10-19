# -*- coding: utf-8 -*-
# Video senaryosu ARTIFACT dokumani uret (video son haline gore). Metinler ses_metinleri'nden okunur.
import os, sys, html
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
SESD=r"D:\KumarFarkindalikOyunu\ses_uretim\video\ses_metinleri"
CIKTI=r"D:\KumarFarkindalikOyunu\video_proje\SENARYO_artifact.html"
def M(ad):
    p=os.path.join(SESD,ad+".txt")
    return open(p,encoding="utf-8-sig").read().strip() if os.path.exists(p) else "(ses yok)"
def para(ad):
    return "".join(f"<p>{html.escape(c.strip())}</p>" for c in M(ad).replace("\n"," ").split(". ") if False) or f"<p>{html.escape(M(ad))}</p>"

# (no, baslik, slaytref, tip, sure, ekran_html, [(etiket,ses_dosya),...])
S=[
 ("01","Kapak","Slayt 1","SLAYT","0:08","Tam ekran kapak slaytı: kurum adı + eğitim başlığı, arka planda kişi ve slot ekranı. Oyun yok.",[("Seslendirme","01_kapak")]),
 ("02","Sanal Kumar Tuzağı","Giriş 01","SLAYT","0:18","Açık zeminli tam ekran slayt: başlık + borçlanma / aile / psikoloji açıklaması.",[("Seslendirme","02_tuzak")]),
 ("03","Kolay Para Görüntüsü","Giriş 02","SLAYT","0:16",'Tam ekran slayt. <span class="vurgu">borç, yalnızlık, kaygı</span> kelimeleri kırmızı vurguyla.',[("Seslendirme","03_kolaypara")]),
 ("04","Simülasyonun Tanıtımı","Giriş 03","SLAYT","0:11","Tam ekran slayt + sağda oyun ekranı görseli (Kumar Kazandırmaz).",[("Seslendirme","04_simulasyon")]),
 ("05","Hilenin Mutfağı — Önizleme","Giriş 04","SLAYT","0:15",'Slayt: "Hilenin Mutfağı · Manipülasyon Paneli" + yönetici paneli görseli.',[("Seslendirme","05_perdearkasi")]),
 ("06","Amaç: Kaybettirmek","Giriş 05","SLAYT","0:23","Tam ekran slayt: dört maddelik süreç özeti.",[("Seslendirme","06_amac")]),
 ("07","Kasa Her Zaman Kazanır","Giriş 06","SLAYT","0:14",'Slayt: "Kasa, her zaman kazanır!" vurgusu.',[("Seslendirme","07_kasa")]),
 ("08","8 Aşama — Geçiş","Ayraç","SLAYT","0:10",'Bölüm ayracı: "8 AŞAMA" + bağımlılık süreci (kazanma/kaybetme eğrisi) grafiği.',[("Seslendirme","08_gecis")]),
 ("09","Aşama 1 — Bal Tuzağı","Aşama 1","SLAYT","0:19",'Slayt: "Bal Tuzağı" + tuzak cümleleri (Hoş geldin bonusu · 2X kazanç · %250 iade) + reklam görseli.',[("Seslendirme","09_asama1_baltuzagi")]),
 ("10","Aşama 2 — Isındırma ve Umut","Aşama 2","SLAYT → OYUN","~0:55",'<b>SLAYT:</b> "Isındırma ve Umut" + bilişsel çarpıtmalar. <b class="kop">→ KÖPRÜ</b> (çubuk sola kayar, oyun büyür). <b class="oyun">OYUN:</b> spin, arka arkaya kazanç, <span class="oyun">Büyük Kazanç 1.531 ₺</span>, 2x çarpan.',[("Seslendirme (slayt)","10_asama2_isindirma"),("Köprü","kopru_oyun")]),
 ("11","Aşama 3 — Kontrol Bende Hissi","Aşama 3","SLAYT","0:16",'Slayt: "Kontrol Bende Hissi" — beceri / kontrol yanılsaması.',[("Seslendirme","11_asama3_kontrol")]),
 ("12","Aşama 4 — Kaybettiklerimi Geri Kazanabilirim","Aşama 4","SLAYT → OYUN","~0:31",'<b>SLAYT:</b> "Kaybettiklerimi Geri Kazanabilirim" + tekrar eden yalan. <b class="kop">→ KÖPRÜ.</b> <b class="oyun">OYUN:</b> spin, <span class="vurgu">bakiye düşer (kayıp)</span>, boş spin sesi — ilk ciddi kayıplar.',[("Seslendirme (slayt)","12_asama4_kayip"),("Köprü","kopru_oyun")]),
 ("13","Aşama 5 — Şansım Döndü","Aşama 5 · ÖZEL","SLAYT → OYUN","~0:56",'<b>SLAYT:</b> "Şansım Döndü" — tuzağa ikinci ısındırma (tanıtım metni). <b class="kop">→ KÖPRÜ.</b> <b class="oyun">OYUN:</b> SPIN\'e basılır → <span class="oyun">100x çarpan ekrana düşer</span> → EFSANE KAZANÇ → <span class="oyun">+50.000 ₺</span>. Tam bu anda <span class="vurgu">çevrim şartı / para çekme tuzağı</span> anlatımı devreye girer (100x ekranda dururken).',[("Seslendirme (slayt · tanıtım)","13a_sansim_tanit"),("Köprü","kopru_oyun"),("Seslendirme (oyun · çevrim şartı)","13b_sansim_cevrim")]),
 ("14","Aşama 6 — Sonunu Düşünen Kahraman Olamaz","Aşama 6","SLAYT → OYUN","~0:38",'<b>SLAYT:</b> "Sonunu Düşünen Kahraman Olamaz" — aldatıcı zirve, bonus/kaldıraç. <b class="kop">→ KÖPRÜ.</b> <b class="oyun">OYUN:</b> <span class="oyun">"ÇOK ŞANSLISIN"</span> teklifi → <span class="vurgu">tüm bakiye bonusa yatırılır</span> → 10 SPIN bonus oyunu (hırs anı).',[("Seslendirme (slayt)","14_asama6_dongu"),("Köprü","kopru_oyun")]),
 ("15","Aşama 7 — Başka Bir Yerden Para Bulmalıyım","Aşama 7","SLAYT","0:20",'Slayt: "Başka Bir Yerden Para Bulmalıyım" — kontrol kaybı, bahane, yalan; borç mesajları görseli.',[("Seslendirme","15_asama7_kontrolkaybi")]),
 ("16","Aşama 8 — Gerçeklerle Yüzleşme","Aşama 8","VİDEO SLAYT","~0:22",'Üstte <b>"Gerçeklerle Yüzleşme · Gelir-gider dengesinin çöküşü"</b> başlığı + altta <b class="oyun">oynayan video</b>: telefonda icra/ev sahibi/borç bildirimleri, elle tutulan gelir-gider defteri (gider ≫ gelir).',[("Seslendirme","16_asama8_tukenis")]),
 ("17","Perde Arkası — Panele Geçiş","Slayt","SLAYT","0:15",'Slayt: <b>"Sıra Hilenin Mutfağı\'nda"</b> + "Manipülasyon Panelini Aç" butonu + oyun görseli.',[("Seslendirme","17_perde_gecis")]),
 ("18","Ama…","Kapanış","SLAYT","0:04",'Tam ekran, beyaz zemin: dev kırmızı <b>"AMA…"</b> + "her şey burada bitmek zorunda değil".',[("Seslendirme","19_ama")]),
 ("19","Yeni Bir Başlangıç","Kapanış","SLAYT","0:14",'Slayt: <b>"Hiçbir şey için geç değil!"</b> + fark etmek / yardım istemek / umut metni.',[("Seslendirme","20_yeni")]),
 ("20","Cesaret","Kapanış","SLAYT","0:12",'Slayt: <b>"Yardım istemek zayıflık değil, CESARETTİR"</b> + Ailenize · Amirlerinize · Güvendiğiniz kişilere.',[("Seslendirme","21_cesaret")]),
 ("21","Kapanış","Kapanış","SLAYT","0:08",'Kapanış: kişi zincirlerinden kurtulur, UMUT · MUTLULUK · GELECEK tabelası + <b>"Kazanan yok; ama fark eden, yardım isteyen ve yeniden başlayan var"</b> + kurum imzası.',[("Seslendirme","22_kapanis")]),
]

def sahne_html(no,bas,ref,tip,sure,ekran,sesler):
    ss=""
    for etk,sd in sesler:
        ss+=f'<div class="ses"><span class="setk">🎙 {html.escape(etk).upper()}</span><p>{html.escape(M(sd))}</p><code>{sd}.txt</code></div>'
    return f'''<div class="sahne">
    <div class="sbas"><span class="sno">{no}</span><span class="sbaslik">{bas}</span><span class="slaytref">{ref}</span><span class="tipr">{tip}</span><span class="sure">{sure}</span></div>
    <div class="satir"><span class="etk">Ekran</span><div class="ekran">{ekran}</div></div>
    <div class="satir"><span class="etk">Ses</span><div class="seswrap">{ss}</div></div>
  </div>'''

giris="".join(sahne_html(*x) for x in S[:8])
asama="".join(sahne_html(*x) for x in S[8:16])
perde=sahne_html(*S[16])
kapanis="".join(sahne_html(*x) for x in S[17:21])

CSS='''<style>
:root{--zemin:#14161b;--yuzey:#1b1e26;--kutu:#191c24;--metin:#e8e6e0;--soft:#a4a29b;--cizgi:rgba(255,255,255,.09);--kirmizi:#e0574a;--kirmizi-zem:rgba(224,87,74,.12);--altin:#d6a94f;--altin-zem:rgba(214,169,79,.12);--yesil:#5fae7e;--mor:#8b7fd6}
@media(prefers-color-scheme:light){:root{--zemin:#f2efe9;--yuzey:#fff;--kutu:#f6f2ea;--metin:#1d1f25;--soft:#63615b;--cizgi:rgba(0,0,0,.10);--kirmizi:#c13b2d;--kirmizi-zem:rgba(193,59,45,.09);--altin:#9e7420;--altin-zem:rgba(158,116,32,.10);--yesil:#2f7d52;--mor:#5a4fb0}}
:root[data-theme=dark]{--zemin:#14161b;--yuzey:#1b1e26;--kutu:#191c24;--metin:#e8e6e0;--soft:#a4a29b;--cizgi:rgba(255,255,255,.09);--kirmizi:#e0574a;--kirmizi-zem:rgba(224,87,74,.12);--altin:#d6a94f;--altin-zem:rgba(214,169,79,.12);--yesil:#5fae7e;--mor:#8b7fd6}
:root[data-theme=light]{--zemin:#f2efe9;--yuzey:#fff;--kutu:#f6f2ea;--metin:#1d1f25;--soft:#63615b;--cizgi:rgba(0,0,0,.10);--kirmizi:#c13b2d;--kirmizi-zem:rgba(193,59,45,.09);--altin:#9e7420;--altin-zem:rgba(158,116,32,.10);--yesil:#2f7d52;--mor:#5a4fb0}
*{box-sizing:border-box}
body{margin:0;background:var(--zemin);color:var(--metin);font-family:-apple-system,"Segoe UI",system-ui,Roboto,sans-serif;line-height:1.6;-webkit-font-smoothing:antialiased}
.sar{max-width:880px;margin:0 auto;padding:0 22px}
header{border-bottom:1px solid var(--cizgi);padding:50px 0 32px;background:linear-gradient(180deg,var(--kirmizi-zem),transparent)}
.eyebrow{font-size:12px;letter-spacing:.22em;text-transform:uppercase;color:var(--kirmizi);font-weight:700;margin:0 0 14px}
h1{font-size:clamp(28px,5vw,42px);line-height:1.08;margin:0 0 10px;font-weight:800;letter-spacing:-.02em;text-wrap:balance}
h1 span{color:var(--altin)}
.altbilgi{color:var(--soft);font-size:15px;max-width:62ch}
.ozet{display:flex;flex-wrap:wrap;gap:10px;margin-top:22px}
.rozet{display:inline-flex;align-items:baseline;gap:7px;background:var(--yuzey);border:1px solid var(--cizgi);border-radius:8px;padding:8px 13px;font-size:13px;color:var(--soft)}
.rozet b{color:var(--metin);font-size:15px;font-weight:700}
.bolum{display:flex;align-items:center;gap:16px;margin:44px 0 20px}
.bolum .bno{font-family:ui-monospace,monospace;font-size:13px;font-weight:700;color:var(--zemin);background:var(--altin);border-radius:6px;padding:4px 9px;flex:none}
.bolum h2{font-size:20px;margin:0;font-weight:800}
.bolum .cizgi{flex:1;height:1px;background:var(--cizgi)}
.sahne{background:var(--yuzey);border:1px solid var(--cizgi);border-radius:14px;padding:20px 22px;margin-bottom:16px}
.sbas{display:flex;align-items:center;gap:11px;flex-wrap:wrap;padding-bottom:14px;margin-bottom:16px;border-bottom:1px solid var(--cizgi)}
.sno{font-family:ui-monospace,monospace;font-weight:700;font-size:14px;color:var(--kirmizi);background:var(--kirmizi-zem);border-radius:6px;padding:3px 9px;flex:none}
.sbaslik{font-weight:700;font-size:16px;flex:1;min-width:170px}
.slaytref{font-size:11.5px;letter-spacing:.06em;text-transform:uppercase;color:var(--soft);flex:none}
.tipr{font-size:11px;font-weight:700;letter-spacing:.04em;color:var(--mor);border:1px solid var(--mor);border-radius:20px;padding:2px 9px;flex:none}
.sure{font-family:ui-monospace,monospace;font-size:12.5px;color:var(--soft);border:1px solid var(--cizgi);border-radius:20px;padding:3px 10px;font-variant-numeric:tabular-nums;flex:none}
.satir{display:flex;gap:12px;margin-bottom:12px}
.satir:last-child{margin-bottom:0}
.etk{font-size:11px;letter-spacing:.1em;text-transform:uppercase;font-weight:700;color:var(--soft);flex:none;width:60px;padding-top:3px}
.ekran{flex:1;color:var(--metin);font-size:14.5px}
.ekran .oyun,.kop.ekran,.oyun{color:var(--altin);font-weight:600}
.ekran .vurgu,.vurgu{color:var(--kirmizi);font-weight:600}
.kop{color:var(--mor);font-weight:700}
.seswrap{flex:1;display:flex;flex-direction:column;gap:8px}
.ses{background:var(--kutu);border:1px solid var(--cizgi);border-left:3px solid var(--altin);border-radius:0 10px 10px 0;padding:12px 15px;font-size:15px;line-height:1.6}
.ses .setk{display:block;font-size:10.5px;letter-spacing:.1em;color:var(--altin);font-weight:700;margin-bottom:6px}
.ses p{margin:0 0 7px}
.ses code{font-size:11px;color:var(--soft);background:var(--zemin);padding:2px 7px;border-radius:5px}
.ipuc{background:var(--altin-zem);border:1px solid var(--cizgi);border-radius:10px;padding:13px 16px;font-size:13.5px;margin:8px 0 22px}
.ipuc b{color:var(--altin)}
.acik{background:var(--kirmizi-zem);border:1px solid var(--cizgi);border-radius:10px;padding:13px 16px;font-size:13.5px;margin:16px 0}
.acik b{color:var(--kirmizi)}
footer{border-top:1px solid var(--cizgi);margin-top:44px;padding:28px 0 56px;color:var(--soft);font-size:13px}
.tema{position:fixed;top:14px;right:14px;z-index:9;background:var(--yuzey);border:1px solid var(--cizgi);color:var(--soft);border-radius:20px;padding:7px 13px;font-size:12px;cursor:pointer;font-family:inherit}
@media(max-width:560px){.satir{flex-direction:column;gap:5px}.etk{width:auto;padding-top:0}}
</style>'''

BODY=f'''{CSS}
<button class="tema" onclick="var r=document.documentElement;var d=(r.getAttribute('data-theme')||(matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light'))==='dark';r.setAttribute('data-theme',d?'light':'dark');">☀ / ☾ tema</button>
<header><div class="sar">
<p class="eyebrow">Video Prodüksiyon Senaryosu · Seslendirme Metinleri</p>
<h1>Kumar Kazandırmaz —<br><span>Sanal Kumar Farkındalık Videosu</span></h1>
<p class="altbilgi">Yapay zeka seslendirmeli eğitim videosunun sahne planı. Her sahnede: ekranda ne göründüğü, oyunda ne olduğu ve okunan metin. <b>Bu doküman videonun güncel montaj haline göre hazırlandı.</b></p>
<div class="ozet"><span class="rozet"><b>21</b> sahne</span><span class="rozet"><b>~7,5</b> dk</span><span class="rozet"><b>4</b> oyun aşaması</span><span class="rozet">Giriş + 8 aşama + kapanış</span></div>
</div></header>
<main class="sar">
<div class="ipuc"><b>Video akış mantığı:</b> Tam ekran <b>SLAYT + seslendirme</b> → köprü cümlesi <span class="kop">"şimdi bunun oyundaki karşılığına bakalım"</span> (çubuk sola kayar, oyun büyür) → <b class="oyun">OYUN</b> görüntüsü + oyun müziği. Oyun aşamaları (2, 4, 5, 6) gerçek oynanış kaydından; slaytlar sunumdan temiz alındı. Ton sakin, ciddi, net.</div>

<div class="bolum"><span class="bno">BÖLÜM 1</span><h2>Açılış ve Giriş</h2><span class="cizgi"></span></div>
{giris}

<div class="bolum"><span class="bno">BÖLÜM 2</span><h2>Simülasyon — 8 Aşamada Çöküş</h2><span class="cizgi"></span></div>
{asama}

<div class="bolum"><span class="bno">BÖLÜM 3</span><h2>Perde Arkası — Panele Geçiş</h2><span class="cizgi"></span></div>
{perde}
<div class="acik"><b>⏳ Panel (Hilenin Mutfağı) bölümü:</b> Manipülasyon panelinde canlı bir "kaybettirme" ayarı gösterilecek — bu bölüm ayrıca çekilecek ve videonun bu noktasına eklenecek. Metni panel çekimiyle birlikte yazılacak.</div>

<div class="bolum"><span class="bno">BÖLÜM 4</span><h2>Umut ve Kapanış</h2><span class="cizgi"></span></div>
{kapanis}
<div class="acik"><b>✅ Kapanış parçası HAZIR:</b> Bu 4 sahne <code>KAPANIS.mp4</code> olarak montajlandı (~39 sn). Panel çekilince 3 parça (giriş+aşamalar · panel · kapanış) birleştirilip tam video çıkacak.</div>

<footer><p><b>Not:</b> Süreler yaklaşıktır; panele kadar toplam ~7 dakika. Cümleleri patronla gözden geçirip değiştirebilirsiniz — her metnin altındaki dosya adı (<code>xx.txt</code>) düzenlenecek ses dosyasını gösterir; metin değişince o ses yeniden üretilip videoya işlenir.</p></footer>
</main>'''
open(CIKTI,"w",encoding="utf-8").write(BODY)
print("OK ->",CIKTI,"(",len(BODY),"bayt )")
