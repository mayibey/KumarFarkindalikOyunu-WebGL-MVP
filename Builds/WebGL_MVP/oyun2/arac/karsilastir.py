# -*- coding: utf-8 -*-
# GORSEL BIREBIRLIK HATTI (Task #13): Unity ve Pixi'yi ayni durumda cek,
# yan yana kompozit + bolge bazli fark yuzdesi (grid ici rastgelelik maskelenir).
import os
from playwright.sync_api import sync_playwright
from PIL import Image, ImageChops, ImageDraw

KOK = r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
UNITY = "http://localhost:8099/oyun/oyun.html"  # SunumPaketi kopyası (br başlıklı sunucu)
PIXI = "http://localhost:8098/oyun2/oyun.html?senaryo"

with sync_playwright() as p:
    b = p.chromium.launch(channel="msedge", headless=True)

    # UNITY: senaryo A1 baslangici (SunumAsamaGit(0) — isim modalsiz taze senaryo)
    u = b.new_page(viewport={"width": 1920, "height": 1080})
    u.goto(UNITY, wait_until="load", timeout=60000)
    u.wait_for_function("() => !!window.unityInstance", timeout=180000)
    u.wait_for_timeout(4000)
    u.evaluate("window.unityInstance.SendMessage('SunumKoprusu','SunumAsamaGit',0)")
    u.wait_for_timeout(12000)
    u.screenshot(path=os.path.join(KOK, "kiyas_unity.png"))
    print("unity cekildi", flush=True)

    # PIXI: ?senaryo baslangici
    x = b.new_page(viewport={"width": 1920, "height": 1080})
    x.goto(PIXI, wait_until="load")
    x.wait_for_timeout(5000)
    x.screenshot(path=os.path.join(KOK, "kiyas_pixi.png"))
    print("pixi cekildi", flush=True)
    b.close()

ui = Image.open(os.path.join(KOK, "kiyas_unity.png")).convert("RGB")
px = Image.open(os.path.join(KOK, "kiyas_pixi.png")).convert("RGB")

# Grid ici rastgele semboller maskelenir (orta bolge) — kiyas UI iskeletine odakli
def maskele(im):
    im = im.copy()
    d = ImageDraw.Draw(im)
    d.rectangle([540, 250, 1420, 790], fill=(0, 0, 0))
    return im

fark = ImageChops.difference(maskele(ui), maskele(px))
hist = fark.convert("L").histogram()
farkli = sum(hist[24:])  # esik: 24+ parlaklik farki olan pikseller
toplam = ui.width * ui.height
print(f"UI iskelet fark orani: %{100*farkli/toplam:.2f} (grid ici haric)")

komp = Image.new("RGB", (1920, 1080 + 8), (30, 30, 30))
komp.paste(ui.resize((960, 540)), (0, 0))
komp.paste(px.resize((960, 540)), (960, 0))
komp.paste(fark.resize((960, 540)), (480, 548))
komp.save(os.path.join(KOK, "kiyas_kompozit.png"))
print("kompozit hazir")
