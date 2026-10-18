@echo off
cd /d "%~dp0"
if not exist "build\ShiftDrag.exe" powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if exist "build\ShiftDrag.exe" start "" "%~dp0build\ShiftDrag.exe"
