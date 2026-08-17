# AIsle Desktop Simulator

AIsle là ứng dụng Windows Desktop mô phỏng hành vi mua sắm của NPC trong cửa
hàng. Hướng sản phẩm active là **.NET 10 + WPF/WebView2 + C# Simulation Core**.

Nguồn quy định:

- [`docs/rule.md`](docs/rule.md): kiến trúc, phạm vi và luật kỹ thuật.
- [`docs/task.md`](docs/task.md): thứ tự task solo và milestone.
- [`docs/log.md`](docs/log.md): nhật ký tiến trình tổng quát, append-only.

## Chạy ứng dụng desktop

Yêu cầu: Windows và .NET SDK 10.

```text
run-desktop.bat
```

Hoặc:

```powershell
dotnet run --project src/AIsle.DesktopApp/AIsle.DesktopApp.csproj --configuration Release
```

Desktop product phải tiến tới chạy in-process, không phụ thuộc Node.js hoặc
localhost. WebView2 local hosting và bridge được triển khai theo các task S1.

## Cấu trúc repository

```text
.
├── src/
│   ├── AIsle.Contracts/       # DTO, schema và contract dùng chung
│   ├── AIsle.Simulation/      # population, GA và simulation core
│   └── AIsle.DesktopApp/      # WPF shell/application/infrastructure/UI
├── tests/
│   ├── AIsle.Population.Tests/
│   ├── AIsle.Simulation.Tests/
│   └── Golden/
├── runtime/                   # layout, catalog và history local
├── web/                       # REFERENCE: web prototype hiện có
├── backend/                   # LEGACY: Node host cho web reference
├── UnityApp/                  # FROZEN: không mở rộng trong MVP
├── mobile/                    # FROZEN: companion sau MVP
└── docs/
```

Phân loại đầy đủ nằm trong [`docs/repository-map.md`](docs/repository-map.md).

## Chạy baseline tests

```powershell
dotnet build AIsle.slnx --configuration Release
dotnet run --project tests/AIsle.Population.Tests/AIsle.Population.Tests.csproj --configuration Release
dotnet run --project tests/AIsle.Simulation.Tests/AIsle.Simulation.Tests.csproj --configuration Release
node --test tests/*.test.mjs
```

## Web prototype reference

`run.bat` chạy Node backend và mở web prototype. Luồng này chỉ là baseline để
đối chiếu/regression trong quá trình chuyển UI sang WebView2 local; nó không phải
runtime mục tiêu của Desktop MVP.

## Phạm vi MVP

```text
Project → Layout → Population → Simulation → Result → Replay → KPI → Compare
```

Reality/Video/Sim-to-Real đã bị loại khỏi roadmap. Mobile, Unity, Emotion mở
rộng, Animation, Spine, Social, ORCA và DOTS được giữ frozen và không được mở
rộng trong MVP.
