# -*- coding: utf-8 -*-
# Sunumdan tum slaytlari (0-15) temiz tam ekran screenshot al. UI gizli.
import time
from playwright.sync_api import sync_playwright
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad\slaytlar"
import os
os.makedirs(SP, exist_ok=True)
URL="file:///D:/KumarFarkindalikOyunu/SunumPaketi/sunum.html"
# gizlenecek UI: alt kontrol bar, hint, sayfa no, bolum etiketi
GIZLE = """() => {
  const sec = ['.hint','.nav','.controls','.toolbar','.pager','.pagenum','.sectionlabel','footer','.footer','.brand','.topbar','.deck-ui','.ui','.chrome'];
  document.querySelectorAll('*').forEach(el=>{
    const t=(el.textContent||'').trim();
    const cs=getComputedStyle(el);
    // alt kosede sabit/absolute kucuk kontroller
  });
  // bilinen: hint yazisi + alt bar. Genel: fixed/absolute pozisyonlu, slide disi elemanlari gizle
  document.querySelectorAll('body > *:not(#deck):not(#oyunPanel)').forEach(el=>{ el.style.visibility='hidden'; });
}"""
with sync_playwright() as p:
    b=p.chromium.launch(channel="msedge")
    pg=b.new_page(viewport={"width":1920,"height":1080}, device_scale_factor=1)
    pg.goto(URL, wait_until="load", timeout=30000)
    pg.wait_for_timeout(2500)
    for i in range(23):
        pg.wait_for_timeout(950)  # rise animasyonu bitsin
        try: pg.evaluate(GIZLE)
        except Exception: pass
        el = pg.query_selector(".slide.active")
        if el:
            el.screenshot(path=os.path.join(SP,f"s_{i:02d}.png"))
        else:
            pg.screenshot(path=os.path.join(SP,f"s_{i:02d}.png"))
        pg.keyboard.press("ArrowRight")
    b.close()
print("bitti - slaytlar cekildi")
