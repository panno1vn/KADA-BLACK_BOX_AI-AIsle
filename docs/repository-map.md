# AIsle Repository Map

> Cập nhật: 2026-08-18  
> Nguồn phân loại: [`rule.md`](./rule.md) và CURRENT TASK S0.1 trong [`task.md`](./task.md).

## Phân loại root

| Path | Trạng thái | Vai trò |
|---|---|---|
| `src/AIsle.DesktopApp` | ACTIVE | Entry point WPF và nơi chứa Application/Bridge/Infrastructure/UI của Desktop MVP. |
| `src/AIsle.Simulation` | ACTIVE | Population/GA, Utility, navigation, interaction và simulation runtime. |
| `src/AIsle.Contracts` | ACTIVE | DTO/schema/contract dùng chung. |
| `tests/AIsle.Population.Tests` | ACTIVE | Population verification. |
| `tests/AIsle.Simulation.Tests` | ACTIVE | Simulation verification. |
| `tests/Golden` | ACTIVE | Golden population scenarios. |
| `runtime` | ACTIVE | Layout, catalog và history local; output history bị Git ignore. |
| `web` | REFERENCE | Web prototype và JavaScript simulation baseline để regression/port UI. |
| `tests/*.mjs` | REFERENCE | Regression gates của web baseline. |
| `backend` | LEGACY | Node HTTP/static host cho web reference; không phải Desktop backend active. |
| `run.bat` | LEGACY | Launcher của web reference. |
| `UnityApp` | FROZEN | Unity source/test cũ; không mở rộng trong MVP. |
| `mobile` | FROZEN | Expo companion sau M7; không mở trong MVP. |
| `docs` | ACTIVE | Rule, task, repository map và log. |

## .NET projects

| Project | Trạng thái | Phụ thuộc trực tiếp |
|---|---|---|
| `src/AIsle.Contracts/AIsle.Contracts.csproj` | ACTIVE | Không có project dependency. |
| `src/AIsle.Simulation/AIsle.Simulation.csproj` | ACTIVE | `AIsle.Contracts`, GeneticSharp DLL, Math.NET DLL. |
| `src/AIsle.DesktopApp/AIsle.DesktopApp.csproj` | ACTIVE | `AIsle.Contracts`, `AIsle.Simulation`, CommunityToolkit.Mvvm, MaterialDesignThemes. |
| `tests/AIsle.Population.Tests/AIsle.Population.Tests.csproj` | ACTIVE | Contracts, Simulation và GeneticSharp. |
| `tests/AIsle.Simulation.Tests/AIsle.Simulation.Tests.csproj` | ACTIVE | Contracts và Simulation. |

## Entry points

| Entry point | Trạng thái | Cách chạy |
|---|---|---|
| `src/AIsle.DesktopApp/App.xaml` | ACTIVE | `run-desktop.bat` hoặc `dotnet run --project src/AIsle.DesktopApp/AIsle.DesktopApp.csproj`. |
| `backend/server.mjs` | LEGACY | `run.bat`; chỉ phục vụ web reference. |
| `web/index.html` | REFERENCE | Được Node legacy host phục vụ. |
| `mobile/App.tsx` | FROZEN | Không chạy trong MVP. |
| `UnityApp/Packages/manifest.json` | FROZEN | Không chạy trong MVP. |

## Source map MVP

```text
run-desktop.bat
  → src/AIsle.DesktopApp
      → src/AIsle.Simulation
          → src/AIsle.Contracts
      → runtime/*.json
```

Target của các task S1:

```text
WPF/WebView2 local UI
  → Application/Bridge in-process
  → C# Simulation Core
  → Local Result Storage
```

## Cleanup đã thực hiện theo yêu cầu trực tiếp

- Xóa Streamlit UI vì không thuộc stack active/reference/frozen trong `rule.md`.
- Xóa `services/VideoAnalytics`, `models` và `data` vì chỉ là skeleton `.gitkeep` của roadmap Reality/Video đã REMOVED.
- Xóa các placeholder rỗng `src/AIsle.Domain`, `src/AIsle.Application`, `src/AIsle.Analytics`, `src/AIsle.Infrastructure`; MVP không tạo project/layer khi chưa có lý do đo được.
- Xóa các placeholder test rỗng `Domain`, `Simulation`, `Integration`, `Video`, `Performance`.
- Xóa `Project.md` và `docs/pic.jpg` vì không được source active hoặc tài liệu canonical tham chiếu.
- Xóa ba ảnh cashier trùng trong Desktop không được XAML tham chiếu; giữ `cashier_idle.jpg` đang dùng và giữ đủ asset của web reference.
- Xóa `.build` sau verification vì đây là output sinh lại được và đã nằm trong `.gitignore`.
- Giữ web/Node reference cho regression; giữ Unity/Mobile frozen theo rule chống rewrite lớn.

## Module đã REMOVED

Repository không còn skeleton active cho Video Analytics, Reality Analytics,
Detection/Tracking, Homography, POS, Observation Schema, Sim-to-Real hoặc
calibration từ dữ liệu thật. Không tạo lại nếu chưa có RFC và phê duyệt explicit.
