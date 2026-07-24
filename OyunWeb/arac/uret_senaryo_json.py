# -*- coding: utf-8 -*-
# ScriptedSenaryo.asset (Unity YAML) -> OyunWeb/veri/senaryo.json
# Birebirlik kurali: veri ELLE YAZILMAZ, bu script kaynaktan uretir.
import json, struct, sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
import yaml

KAYNAK = r"D:\KumarFarkindalikOyunu\Assets\_Project\Resources\ScriptedSenaryo.asset"
HEDEF = r"D:\KumarFarkindalikOyunu\OyunWeb\veri\senaryo.json"

with open(KAYNAK, encoding="utf-8") as f:
    metin = f.read()
# Unity YAML basligini standart YAML'a indir
satirlar = [s for s in metin.splitlines() if not s.startswith("%")]
metin = "\n".join(satirlar).replace("--- !u!114 &11400000", "---")
# Hex bloblarini tirnakla: salt rakamli olanlari YAML int sanip bastaki sifirlari yutuyor
metin = re.sub(
    r"(ilkGridSemboller|ilkCarpanDegerleri|yukaridanDusenSemboller|yukaridanDusenCarpanlar): ([0-9a-fA-F]+)\s*$",
    r'\1: "\2"',
    metin, flags=re.M)
dok = yaml.safe_load(metin)
mb = dok["MonoBehaviour"]

def hex_int32_dizi(hexs):
    if not hexs:
        return []
    b = bytes.fromhex(hexs)
    return list(struct.unpack("<" + "i" * (len(b) // 4), b))

def spin_cevir(s):
    return {
        "sira": s["spinSiraNo"],
        "asama": s["asamaIndex"],
        "bahis": s["bahis"],
        "tip": s["tip"],  # 0=Sifir 1=NearMiss 2=Kazanc 3=MegaWin 4=BonusTetik 5=BahisIadesi
        "grid": hex_int32_dizi(s.get("ilkGridSemboller", "")),
        "carpanlar": hex_int32_dizi(s.get("ilkCarpanDegerleri", "")),
        "tumbleler": [
            {
                "patlayan": [[h["x"], h["y"]] for h in (t.get("patlayanHucreler") or [])],
                "dusenSemboller": hex_int32_dizi(t.get("yukaridanDusenSemboller", "")),
                "dusenCarpanlar": hex_int32_dizi(t.get("yukaridanDusenCarpanlar", "")),
            }
            for t in (s.get("tumbleler") or [])
        ],
        "modal": s.get("modalMesaji", "") or "",
        "carpanKacti": bool(s.get("carpanKactiFlag", 0)),
        "bonusTetik": bool(s.get("bonusOyunuTetikle", 0)),
        "bonusGetirisi": s.get("bonusGetirisi", 0),
    }

cikti = {
    "_kaynak": "Assets/_Project/Resources/ScriptedSenaryo.asset (uret_senaryo_json.py ile uretildi — elle duzenleme YASAK)",
    "asamaSpinleri": [[spin_cevir(sp) for sp in (a.get("spinler") or [])] for a in mb["asamaSpinleri"]],
    "bonusSpinleri": [spin_cevir(sp) for sp in (mb.get("bonusSpinleri") or [])],
}

# Dogrulama
beklenen = [8, 8, 8, 5, 5, 0, 0]
gercek = [len(a) for a in cikti["asamaSpinleri"]]
hatalar = []
if gercek != beklenen:
    hatalar.append(f"asama spin sayilari {gercek} != beklenen {beklenen}")
if len(cikti["bonusSpinleri"]) != 10:
    hatalar.append(f"bonus spin {len(cikti['bonusSpinleri'])} != 10")
for ai, asama in enumerate(cikti["asamaSpinleri"]):
    for sp in asama:
        if len(sp["grid"]) != 30: hatalar.append(f"A{ai}S{sp['sira']}: grid {len(sp['grid'])} != 30")
        if len(sp["carpanlar"]) != 30: hatalar.append(f"A{ai}S{sp['sira']}: carpan {len(sp['carpanlar'])} != 30")
        for ti, t in enumerate(sp["tumbleler"]):
            if len(t["dusenSemboller"]) != len(t["patlayan"]):
                hatalar.append(f"A{ai}S{sp['sira']} T{ti}: dusen {len(t['dusenSemboller'])} != patlayan {len(t['patlayan'])}")
toplam_bonus = sum(sp["bahis"] for sp in cikti["bonusSpinleri"])

import os
os.makedirs(os.path.dirname(HEDEF), exist_ok=True)
with open(HEDEF, "w", encoding="utf-8") as f:
    json.dump(cikti, f, ensure_ascii=False, separators=(",", ":"))

print("asama spinleri:", gercek, "bonus:", len(cikti["bonusSpinleri"]))
print("ornek A0S1 grid[0:6]:", cikti["asamaSpinleri"][0][0]["grid"][:6])
print("ornek modal:", cikti["asamaSpinleri"][0][0]["modal"][:80])
print("HATALAR:", hatalar if hatalar else "YOK")
import os
print(f"yazildi: {HEDEF} ({os.path.getsize(HEDEF)/1024:.0f} KB)")
