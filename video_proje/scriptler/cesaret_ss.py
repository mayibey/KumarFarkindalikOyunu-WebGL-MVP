# -*- coding: utf-8 -*-
from playwright.sync_api import sync_playwright
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
with sync_playwright() as p:
    b=p.chromium.launch(channel="msedge")
    pg=b.new_page(viewport={"width":1920,"height":1080}, device_scale_factor=1)
    pg.goto("file:///"+SP.replace("\\","/")+"/cesaret_slayt.html", wait_until="load")
    pg.wait_for_timeout(900)
    pg.screenshot(path=SP+r"\cesaret_yeni.png")
    b.close()
print("ok")
