@echo off
setlocal
cd /d "%~dp0"
echo Khoi chay ung dung AIsle Desktop App...
if exist "Builds\Desktop\AIsleDesktop.exe" (
  start "" "Builds\Desktop\AIsleDesktop.exe"
) else (
  dotnet run --project src\AIsle.DesktopApp\AIsle.DesktopApp.csproj
)
endlocal
