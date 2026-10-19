@echo off
title Kumar Farkindalik Oyunu - Sunum
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sunucu.ps1"
pause
