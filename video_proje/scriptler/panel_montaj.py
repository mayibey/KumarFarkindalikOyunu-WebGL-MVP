# -*- coding: utf-8 -*-
# PANEL montaj: pnl1 modlari sirayla dondur + pnl2 carpan(100x kazanc) + pnl3 500x(odeme yok) + fon muzik/carpan efekti. Gapless.
import subprocess, os, sys
try: sys.stdout.reconfigure(encoding="utf-8")
except Exception: pass
FF=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffmpeg.exe"
FP=r"C:\Users\GIGABYTE\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffprobe.exe"
SP=r"C:\Users\GIGABYTE\AppData\Local\Temp\claude\D--KumarFarkindalikOyunu\48ce8bc9-c3fc-465a-b453-05da09283cd5\scratchpad"
MK=os.path.join(SP,"modkare")
KAYIT=os.path.join(SP,"panel_kayit.mp4")
FON=r"D:\KumarFarkindalikOyunu\OyunWeb\varlik\ses\fon_muzigi.mp3"
SESD=r"D:\KumarFarkindalikOyunu\ses_uretim\video\ses dosyaları"
def run(a): subprocess.run([FF,"-hide_banner","-loglevel","error","-y"]+a, check=True)
def sure(f): return float(subprocess.check_output([FP,"-v","error","-show_entries","format=duration","-of","csv=p=0",f]).decode().strip())
def o(n): return os.path.join(SP,n)
def se(n): return os.path.join(SESD,n)
SIL="silenceremove=start_periods=1:start_threshold=-38dB:start_silence=0.05:stop_periods=-1:stop_threshold=-38dB:stop_duration=0.35"
ENCV=["-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p","-r","30","-s","1920x1080"]
ENCA=["-c:a","aac","-b:a","192k","-ar","44100","-ac","2"]
def tses(ses,out): run(["-i",se(ses),"-af",SIL]+ENCA+[out]); return sure(out)

# ===== PNL1: modlari sirayla dondur =====
sp1=o("_pnl1.m4a"); S1=tses("pnl1_modlar.mp3",sp1)
# segment sureleri (karakter oranina gore, toplam S1)
segs=[("m_48.png",0.375),("m_50.png",0.128),("m_51,5.png",0.145),("m_53.png",0.193),("m_54.png",0.159)]
segf=[]
acc=0
for i,(png,oran) in enumerate(segs):
    sn = (S1-acc) if i==len(segs)-1 else round(S1*oran,2); acc+=sn
    f=o(f"_p1s{i}.mp4"); run(["-loop","1","-i",os.path.join(MK,png),"-t",f"{sn:.2f}"]+ENCV+["-an",f]); segf.append(f)
# concat segment videolari
lst1=o("_p1list.txt"); open(lst1,"w").write("".join(f"file '{x.replace(chr(92),'/')}'\n" for x in segf))
p1v=o("_p1v.mp4"); run(["-f","concat","-safe","0","-i",lst1,"-c","copy",p1v])
# ses: pnl1 ustte + fon muzik altta (kisik)
pnl1=o("mpnl1.mp4")
run(["-i",p1v,"-i",sp1,"-stream_loop","-1","-i",FON,
     "-filter_complex","[1:a]volume=1.3[v];[2:a]volume=0.12[f];[v][f]amix=inputs=2:duration=first[a]",
     "-map","0:v","-map","[a]","-t",f"{S1:.2f}"]+ENCV+ENCA+[pnl1])

# ===== PNL2: carpan dusur -> 100x -> 30.000 kazanc (canli) =====
sp2=o("_pnl2.m4a"); S2=tses("pnl2_carpan.mp3",sp2)
# kayit 58-76s carpan+100x+EFSANE KAZANC; sure S2'ye gore
pnl2=o("mpnl2.mp4")
run(["-ss","58","-t",f"{S2:.2f}","-i",KAYIT,"-i",sp2,
     "-filter_complex",f"[0:v]scale=1920:1080,setsar=1[v];[1:a]volume=1.35[an];[0:a]volume=0.5[oy];[an][oy]amix=inputs=2:duration=first[a]",
     "-map","[v]","-map","[a]","-t",f"{S2:.2f}"]+ENCV+ENCA+[pnl2])

# ===== PNL3: 500x dusur -> odeme yok (canli + dondur) =====
sp3=o("_pnl3.m4a"); S3=tses("pnl3_yakinkacir.mp3",sp3)
# kayit 76-88s (12s canli) + son kare dondur S3'e kadar
oyun_dur=12.0; fr=max(0.0,S3-oyun_dur)
pnl3=o("mpnl3.mp4")
run(["-ss","76","-t",f"{oyun_dur}","-i",KAYIT,"-i",sp3,
     "-filter_complex",f"[0:v]tpad=stop_mode=clone:stop_duration={fr:.2f},scale=1920:1080,setsar=1[v];[1:a]volume=1.35[an];[0:a]volume=0.5,apad[oy];[an][oy]amix=inputs=2:duration=first[a]",
     "-map","[v]","-map","[a]","-t",f"{S3:.2f}"]+ENCV+ENCA+[pnl3])

# ===== GAPLESS BIRLESTIR =====
parts=[pnl1,pnl2,pnl3]
inp=[]
for p in parts: inp+=["-i",p]
n=len(parts)
vf="".join(f"[{i}:v]scale=1920:1080,setsar=1,fps=30,format=yuv420p[v{i}];" for i in range(n))
af="".join(f"[{i}:a]aformat=sample_rates=44100:channel_layouts=stereo,asetpts=N/SR/TB[a{i}];" for i in range(n))
cc="".join(f"[v{i}][a{i}]" for i in range(n))
out=o("PANEL.mp4")
run(inp+["-filter_complex",f"{vf}{af}{cc}concat=n={n}:v=1:a=1[v][a]","-map","[v]","-map","[a]"]+ENCV+ENCA+["-movflags","+faststart",out])
print("PANEL OK",round(sure(out),1),"sn  (pnl1",round(S1,1),"+pnl2",round(S2,1),"+pnl3",round(S3,1),")")
