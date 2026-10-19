# -*- coding: utf-8 -*-
# Tek dosya TTS yeniden uret (uzerine yazar). Arg: stem (uzantisiz).
import sys, os
from pathlib import Path
import requests
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass

KOK="D:\\KumarFarkindalikOyunu\\ses_uretim\\video"
ENV_PATH="D:\\JSPS Fabrika\\.env"
VOICE_ID="jNEAGPibhhLMspJ6h1ow"; MODEL_ID="eleven_flash_v2_5"; OUTPUT_FORMAT="mp3_44100_128"
STABILITY=0.5; SIMILARITY=0.75; SPEAKER_BOOST=True

def api_key():
    p=Path(ENV_PATH)
    if p.exists():
        for s in p.read_text(encoding="utf-8-sig").splitlines():
            s=s.strip()
            if not s or s.startswith("#") or "=" not in s: continue
            k,_,v=s.partition("=")
            if k.strip()=="ELEVENLABS_API_KEY": return v.strip().strip('"').strip("'")
    return os.environ.get("ELEVENLABS_API_KEY","").strip()

stem=sys.argv[1]
txt=Path(KOK)/"ses_metinleri"/(stem+".txt")
mp3=Path(KOK)/"ses dosyaları"/(stem+".mp3")
metin=txt.read_text(encoding="utf-8-sig").strip()
print(f"Metin ({len(metin)} krk): {metin[:60]}...")
url=f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}?output_format={OUTPUT_FORMAT}"
r=requests.post(url, headers={"xi-api-key":api_key(),"Content-Type":"application/json"},
    json={"text":metin,"model_id":MODEL_ID,"voice_settings":{"stability":STABILITY,"similarity_boost":SIMILARITY,"use_speaker_boost":SPEAKER_BOOST}}, timeout=120)
if r.status_code==200:
    mp3.write_bytes(r.content)
    print(f"OK -> {mp3}  ({len(r.content)//1024} KB)")
else:
    print(f"HATA {r.status_code}: {r.text[:150]}")
