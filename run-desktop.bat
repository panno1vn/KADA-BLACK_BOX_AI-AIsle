@echo off
setlocal
cd /d "%~dp0"
set "DESKTOP_PROJECT=src\AIsle.DesktopApp\AIsle.DesktopApp.csproj"

echo Khoi chay ung dung AIsle Desktop App...
powershell -NoProfile -Command "if (Get-Process -Name 'AIsleDesktop' -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
if not errorlevel 1 (
  echo AIsle Desktop dang chay. Khong build lai de tranh khoa file AIsleDesktop.exe.
  exit /b 0
)

where dotnet >nul 2>nul || (
  echo Khong tim thay .NET SDK 10. Vui long cai .NET SDK 10 truoc khi chay.
  pause
  exit /b 1
)

if not exist "%DESKTOP_PROJECT%" (
  echo Khong tim thay project: %DESKTOP_PROJECT%
  pause
  exit /b 1
)

dotnet run --project "%DESKTOP_PROJECT%" --configuration Release
if errorlevel 1 (
  echo.
  echo AIsle Desktop khoi dong that bai. Xem thong bao loi o tren.
  pause
  exit /b 1
)
endlocal
