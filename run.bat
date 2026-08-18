@echo off
setlocal
cd /d "%~dp0"
set "NODE_EXE=node"
where node >nul 2>nul || set "NODE_EXE=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
if not exist "%NODE_EXE%" where node >nul 2>nul || (echo Node.js 22+ is required & pause & exit /b 1)
powershell -NoProfile -Command "try { Invoke-WebRequest 'http://127.0.0.1:8765/health' -UseBasicParsing -TimeoutSec 1 ^| Out-Null; exit 0 } catch { exit 1 }"
if errorlevel 1 (
  start "AIsle Backend" /min "%NODE_EXE%" backend\server.mjs
  timeout /t 1 /nobreak >nul
)
rem Optional: tab "😊 Cảm Xúc" (demo only — see services/VideoAnalytics/EmotionRecognition/README.md).
rem Skipped silently if the Python venv there hasn't been set up; the tab just shows how to start it.
if exist "venv\Scripts\python.exe" (
  powershell -NoProfile -Command "try { Invoke-WebRequest 'http://127.0.0.1:8801/health' -UseBasicParsing -TimeoutSec 1 ^| Out-Null; exit 0 } catch { exit 1 }"
  if errorlevel 1 (
    start "AIsle Emotion Service" /min venv\Scripts\python.exe -m uvicorn services.VideoAnalytics.EmotionRecognition.emotion_service:app --host 127.0.0.1 --port 8801
  )
)
start "" "http://127.0.0.1:8765"
endlocal
