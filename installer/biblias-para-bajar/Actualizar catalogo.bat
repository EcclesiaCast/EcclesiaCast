@echo off
rem Doble clic: actualiza catalogo.json con los .ecbiblias de esta carpeta.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Actualizar catalogo.ps1"
echo.
pause
