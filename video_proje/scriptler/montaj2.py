# -*- coding: utf-8 -*-
# MASTER MONTAJ v2: kesit duzeltmeleri + A5 senkron + Asama 8 video + panel-git.
import subprocess, os, sys
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
FF=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffmpeg.exe"
FP=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffprobe.exe"
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
SL=os.path.join(SP,"slaytlar")
KAYIT=os.path.join(SP,"kayit.mp4")
SESD=r"D:\KumarFarkindalikOyunu\ses_uretim\video\ses dosyaları"
KOP=os.path.join(SESD,"kopru_oyun.mp3")
MEDIA2=r"D:\KumarFarkindalikOyunu\SunumPaketi\media\media2.mp4"
def run(a): subprocess.run([FF,"-hide_banner","-loglevel","error","-y"]+a, check=True)
def sure(f): return float(subprocess.check_output([FP,"-v","error","-show_entries","format=duration","-of","csv=p=0",f]).decode().strip())
def sl(n): return os.path.join(SL,f"s_{n:02d}.png")
def se(n): return os.path.join(SESD,n)
def o(n): return os.path.join(SP,n)
ENC=["-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p","-r","30","-s","1920x1080","-c:a","aac","-b:a","192k","-ar","44100","-ac","2"]

def slayt_only(png, ses, out):
    run(["-loop","1","-i",png,"-i",se(ses)]+ENC+["-shortest",out])

def oyun_sahne(png, anlat, oyun_ss, oyun_dur, out, ozel13b=None, gecikme=0, freeze=0, oyunvol="1.3", oyunvol_ozel="0.45"):
    # slayt_part sesi: anlat + kopru + 0.6s sessizlik (kopru cakismasin)
    spses=out+".ss.m4a"
    run(["-i",se(anlat),"-i",KOP,"-filter_complex","[0:a][1:a]concat=n=2:v=0:a=1,apad=pad_dur=0.6[a]","-map","[a]","-c:a","aac","-b:a","192k",spses])
    S=sure(spses)
    slaytp=out+".sl.mp4"
    run(["-loop","1","-i",png,"-i",spses]+ENC+["-shortest",slaytp])
    oyunp=out+".oy.mp4"
    if ozel13b:
        run(["-ss",str(oyun_ss),"-t",str(oyun_dur),"-i",KAYIT,"-i",se(ozel13b),
             "-filter_complex",f"[0:v]tpad=stop_mode=clone:stop_duration={freeze},scale=1920:1080,setsar=1[v];[0:a]volume={oyunvol_ozel},apad[oy];[1:a]adelay={gecikme}|{gecikme},volume=1.45[an];[oy][an]amix=inputs=2:duration=longest[a]",
             "-map","[v]","-map","[a]","-t",str(oyun_dur+freeze)]+ENC+[oyunp])
    else:
        run(["-ss",str(oyun_ss),"-t",str(oyun_dur),"-i",KAYIT,"-vf","scale=1920:1080,setsar=1","-af",f"volume={oyunvol}"]+ENC+[oyunp])
    run(["-i",slaytp,"-i",oyunp,
         "-filter_complex",f"[0:v][1:v]xfade=transition=slideleft:duration=1:offset={S-1:.2f}[v];[0:a][1:a]acrossfade=d=0.8[a]",
         "-map","[v]","-map","[a]"]+ENC+[out])

def video_sahne(video, anlat, out):
    V=sure(video); A=sure(se(anlat)); fr=max(0.0, A-V+0.6)
    run(["-i",video,"-i",se(anlat),
         "-filter_complex",f"[0:v]tpad=stop_mode=clone:stop_duration={fr},scale=1920:1080,setsar=1,fps=30[v];[0:a]volume=0.12[vs];[1:a]volume=1.35[an];[vs][an]amix=inputs=2:duration=longest[a]",
         "-map","[v]","-map","[a]","-t",str(A+0.6)]+ENC+[out])

parts=[]
def ekle(x): parts.append(x)
# GIRIS
for i,ses in [(0,"01_kapak.mp3"),(1,"02_tuzak.mp3"),(2,"03_kolaypara.mp3"),(3,"04_simulasyon.mp3"),(4,"05_perdearkasi.mp3"),(5,"06_amac.mp3"),(6,"07_kasa.mp3"),(7,"08_gecis.mp3")]:
    p=o(f"g{i}.mp4"); slayt_only(sl(i),ses,p); ekle(p)
# ASAMA 1 (slayt)
p=o("s09.mp4"); slayt_only(sl(9),"09_asama1_baltuzagi.mp3",p); ekle(p)
# ASAMA 2 (OYUN) - degismedi (iyiydi)
p=o("A2.mp4"); oyun_sahne(sl(10),"10_asama2_isindirma.mp3",106,28,p); ekle(p)
# ASAMA 3 (slayt)
p=o("s11.mp4"); slayt_only(sl(11),"11_asama3_kontrol.mp3",p); ekle(p)
# ASAMA 4 (OYUN) - kesit 186-198 (spin/kayip/dadadant), oyun sesi yuksek
p=o("A4.mp4"); oyun_sahne(sl(12),"12_asama4_kayip.mp3",186,12,p,oyunvol="1.8"); ekle(p)
# ASAMA 5 (OYUN OZEL) - kesit 237-256 (100x dusus+kazanc+grid), 13b kazanc anina gecikmeli, oyun sesi acik
p=o("A5.mp4"); oyun_sahne(sl(13),"13a_sansim_tanit.mp3",230,26,p,ozel13b="13b_sansim_cevrim.mp3",gecikme=13000,freeze=16); ekle(p)
# ASAMA 6 (OYUN) - kesit 289-305 (500x -> cok sanslisin -> bonus)
p=o("A6.mp4"); oyun_sahne(sl(14),"14_asama6_dongu.mp3",289,16,p,oyunvol="1.5"); ekle(p)
# ASAMA 7 (slayt)
p=o("s15.mp4"); slayt_only(sl(15),"15_asama7_kontrolkaybi.mp3",p); ekle(p)
# ASAMA 8 TUKENIS (video + anlatim)
p=o("A8.mp4"); video_sahne(o("a8_slayt.mp4"),"16_asama8_tukenis.mp3",p); ekle(p)
# PANEL-GIT (slayt)
p=o("s18.mp4"); slayt_only(sl(18),"17_perde_gecis.mp3",p); ekle(p)

lst=o("concat_tam.txt")
with open(lst,"w",encoding="utf-8") as f:
    for x in parts: f.write("file '"+x.replace("\\","/")+"'\n")
out=o("VIDEO_TAM.mp4")
run(["-f","concat","-safe","0","-i",lst,"-r","30","-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p","-c:a","aac","-b:a","192k","-movflags","+faststart",out])
print("OK ->",out," sure=",round(sure(out),1),"sn")
