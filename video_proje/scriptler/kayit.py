# -*- coding: utf-8 -*-
# Ekran + Stereo Mix kaydi. dur.flag olusunca ffmpeg'e 'q' gonderip mp4'u finalize eder.
import subprocess, time, os, sys
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
FF=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffmpeg.exe"
AUDIO="Stereo Karışımı (Realtek(R) Audio)"
out=os.path.join(SP,"kayit.mp4")
flag=os.path.join(SP,"dur.flag")
if os.path.exists(flag): os.remove(flag)
if os.path.exists(out):
    try: os.remove(out)
    except Exception: pass
cmd=[FF,"-y","-f","gdigrab","-framerate","30","-i","desktop",
     "-f","dshow","-i",f"audio={AUDIO}",
     "-c:v","libx264","-preset","veryfast","-crf","14","-pix_fmt","yuv420p",
     "-c:a","aac","-b:a","256k","-movflags","+faststart",out]
p=subprocess.Popen(cmd, stdin=subprocess.PIPE)
print("KAYIT BASLADI PID", p.pid, flush=True)
t0=time.time()
while True:
    if os.path.exists(flag): break
    if time.time()-t0>1200: break        # 20 dk guard
    if p.poll() is not None: break
    time.sleep(0.4)
try:
    p.stdin.write(b'q'); p.stdin.flush()
except Exception: pass
try: p.wait(timeout=20)
except Exception:
    try: p.terminate()
    except Exception: pass
sz=os.path.getsize(out) if os.path.exists(out) else 0
print(f"KAYIT DURDU -> {out}  ({sz//1024} KB)", flush=True)
