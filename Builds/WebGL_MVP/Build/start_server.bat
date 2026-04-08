@echo off
cd /d %~dp0
powershell -Command "Start-Process http://localhost:8080"
powershell -Command "Start-Process powershell -ArgumentList '-NoExit','-Command','cd \"%cd%\"; Start-Process -FilePath python -ArgumentList \"-m http.server 8080\"'"
pause