# -*- coding: utf-8 -*-
# FINAL: VIDEO_TAM (giris+asamalar+panel-git) + PANEL + KAPANIS -> tek video, gapless (concat filter, CFR).
import subprocess, os, sys
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
FF=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffmpeg.exe"
FP=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffprobe.exe"
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
def o(n): return os.path.join(SP,n)
parts=[o("VIDEO_TAM.mp4"), o("PANEL.mp4"), o("KAPANIS.mp4")]
for p in parts:
    if not os.path.exists(p): print("EKSIK:",p); sys.exit(1)
inp=[]
for p in parts: inp+=["-i",p]
n=len(parts)
vf="".join(f"[{i}:v]scale=1920:1080,setsar=1,fps=30,format=yuv420p[v{i}];" for i in range(n))
af="".join(f"[{i}:a]aformat=sample_rates=44100:channel_layouts=stereo,asetpts=N/SR/TB[a{i}];" for i in range(n))
cc="".join(f"[v{i}][a{i}]" for i in range(n))
out=o("FINAL_TAM.mp4")
subprocess.run([FF,"-hide_banner","-loglevel","error","-y"]+inp+["-filter_complex",f"{vf}{af}{cc}concat=n={n}:v=1:a=1[v][a]",
     "-map","[v]","-map","[a]","-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p","-r","30",
     "-c:a","aac","-b:a","192k","-ar","44100","-ac","2","-movflags","+faststart",out], check=True)
d=float(subprocess.check_output([FP,"-v","error","-show_entries","format=duration","-of","csv=p=0",out]).decode().strip())
print(f"FINAL_TAM OK -> {out}  {round(d,1)} sn ({round(d/60,1)} dk)")
