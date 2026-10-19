# -*- coding: utf-8 -*-
# KAPANIS parcasi: AMA -> Yeni Baslangic -> Cesaret -> Kapanis (slayt + ses). 3. parca, ayri.
import subprocess, os, sys
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
FF=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffmpeg.exe"
FP=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffprobe.exe"
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
SL=os.path.join(SP,"slaytlar")
SESD=r"D:\KumarFarkindalikOyunu\ses_uretim\video\ses dosyaları"
def run(a): subprocess.run([FF,"-hide_banner","-loglevel","error","-y"]+a, check=True)
def sure(f): return float(subprocess.check_output([FP,"-v","error","-show_entries","format=duration","-of","csv=p=0",f]).decode().strip())
def sl(n): return os.path.join(SL,f"s_{n:02d}.png")
def se(n): return os.path.join(SESD,n)
def o(n): return os.path.join(SP,n)
ENC=["-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p","-r","30","-s","1920x1080","-c:a","aac","-b:a","192k","-ar","44100","-ac","2"]
def slayt_only(png, ses, out):
    run(["-loop","1","-i",png,"-i",se(ses)]+ENC+["-shortest",out])
parts=[]
for idx,ses in [(19,"19_ama.mp3"),(20,"20_yeni.mp3"),(21,"21_cesaret.mp3"),(22,"22_kapanis.mp3")]:
    p=o(f"k{idx}.mp4"); slayt_only(sl(idx),ses,p); parts.append(p)
lst=o("concat_kapanis.txt")
with open(lst,"w",encoding="utf-8") as f:
    for x in parts: f.write("file '"+x.replace("\\","/")+"'\n")
out=o("KAPANIS.mp4")
run(["-f","concat","-safe","0","-i",lst,"-r","30","-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p","-c:a","aac","-b:a","192k","-movflags","+faststart",out])
print("OK ->",out," sure=",round(sure(out),1),"sn")
