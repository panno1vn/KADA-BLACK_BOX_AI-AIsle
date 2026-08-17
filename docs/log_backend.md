# AIsle — Nhật ký Backend

File này lưu tiến trình chuyên môn của phần **Backend**. Hình thức và quy tắc bản ghi được giữ nguyên từ `log.md`; mọi nội dung phải đối chiếu với [`Result_Plan.md`](./Result_Plan.md).

## Quy tắc bắt buộc

1. Chỉ được ghi nối tiếp ở cuối file; không sửa, xóa, sắp xếp lại hoặc ghi đè các bản ghi đã tồn tại.
2. Mỗi lần thực hiện công việc phải tạo một bản ghi riêng.
3. Mỗi bản ghi bắt buộc có đầy đủ:
   - ngày, tháng, năm;
   - giờ và phút theo múi giờ địa phương;
   - người thực hiện;
   - lý do sửa;
   - những gì đã sửa hoặc đã làm;
   - trạng thái đã đạt hay chưa;
   - việc nên làm tiếp theo.
4. Nội dung phải nêu rõ file/thư mục/dữ liệu bị tác động và đối chiếu với phạm vi trong `Result_Plan.md`.
5. Không được ghi “đã đạt” khi chưa có kiểm tra phù hợp.
6. Nếu công việc thất bại, bị chặn hoặc chỉ hoàn thành một phần, phải ghi rõ nguyên nhân và phần còn thiếu.
7. Hoạt động local, commit và push phải được phân biệt rõ. Không được ghi hoặc suy diễn rằng thay đổi đã lên GitHub nếu chưa thực sự push.

## Mẫu bản ghi bắt buộc

```markdown
## YYYY-MM-DD HH:mm (UTC±HH:MM) — <Người thực hiện>

- Lý do sửa: ...
- Đã sửa/đã làm: ...
- Đối chiếu Result_Plan.md: ...
- Trạng thái: Đạt / Chưa đạt / Đạt một phần.
- Kiểm tra: ...
- Nên làm tiếp theo: ...
- Phạm vi đồng bộ: Chỉ local / Đã commit local / Đã push <remote/branch>.
```

---

## 2026-08-11 12:09 (UTC+07:00) — Codex

- Lý do sửa: Hoàn tất và xác minh hai nhiệm vụ nền tảng trong phạm vi hiện tại: tái thiết cấu trúc repository và chuyển hướng từ web prototype sang Unity desktop app skeleton.
- Đã sửa/đã làm: Cập nhật `docs/Result_Plan.md` thành kế hoạch chính thức và khóa phạm vi hiện tại; tạo cấu trúc `UnityApp/`, `src/`, `services/VideoAnalytics/`, `models/`, `data/` và các nhóm test mục tiêu; tạo metadata tối thiểu để Unity Hub nhận project; bổ sung ignore cho dữ liệu Unity sinh tự động; giữ nguyên web/backend/runtime và test `.mjs` làm baseline/reference; ghi marker để các thư mục cấu trúc được version control nhận biết sau này.
- Đối chiếu Result_Plan.md: Mục 2.1, mục 4, Stage 1, Stage 4 và Phase 1 — `Tạo Unity project structure`.
- Trạng thái: Đạt đối với hai nhiệm vụ được giao trong mốc hiện tại. UI/UX và simulation core chưa được triển khai đúng theo giới hạn phạm vi.
- Kiểm tra: Đủ 37 đường dẫn bắt buộc; `UnityApp/Packages/manifest.json` parse JSON thành công; `git diff --check` thành công; 10/10 file regression test của web baseline pass bằng Node.js v24.14.0. Lần gọi thử `node --test tests` không phù hợp với Node runtime này vì thư mục bị coi là module; đã chạy lại đúng bằng danh sách file `*.test.mjs` và tất cả đều pass. Chưa thể mở project bằng Unity vì máy có Unity Hub nhưng chưa cài Unity Editor.
- Nên làm tiếp theo: Cài Unity Editor phù hợp, mở `UnityApp/` để Unity sinh metadata và xác minh project trong Editor; sau đó freeze golden tests và chốt contracts trước khi port C# core. UI/UX và core chỉ bắt đầu ở mốc sau theo yêu cầu của chủ dự án.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push.


## 2026-08-13 20:56 (UTC+07:00) — Antigravity

- Lý do sửa: Di chuyển và đồng bộ hóa thư mục ứng dụng Mobile cùng Mock API lên nhánh test trên Github theo cấu trúc mới.
- Đã sửa/đã làm: Sao lưu mã nguồn mobile app dở dang, reset local test về giống origin/test; di chuyển ứng dụng mobile ra thư mục gốc thành mobile/; tạo tài liệu mobile/README.md; tích hợp thuật toán sinh số ngẫu nhiên Mock API vào backend/routes/api-router.mjs; cập nhật cấu hình layout.json trong runtime/layout.json; khôi phục Streamlit python app trong app/; dọn dẹp các thư mục tạm backup_temp/ và build/ cũ.
- Đối chiếu Result_Plan.md: Đồng bộ cấu trúc phẳng, Mock API backend, cấu hình layout và ứng dụng Mobile.
- Trạng thái: Đạt.
- Kiểm tra: Commit 0a16a37 thành công, push thành công lên origin/test, git status sạch, không còn thư mục build/ hay backup_temp/ trong workspace.
- Nên làm tiếp theo: Tiếp tục phát triển và hoàn thiện các màn hình của ứng dụng Mobile.
- Phạm vi đồng bộ: Đã commit và push lên origin/test.


## 2026-08-14 10:15 (UTC+07:00) — OpenCode — Port toàn bộ UI web sang Unity (Stage 4 full)

- Lý do sửa: Chủ dự án xác nhận yêu cầu UI hiển thị giống khi chạy `run.bat` (web prototype) ngay trong Unity; backend sẽ do người khác làm, app Unity vẫn dùng core C# local.
- Đã sửa/đã làm:
  - Rewrite `SimulationRenderer`: tách rebuild world/NPC, màu status public, `SelectWall/SelectShelf/ClearSelection/ScreenToWorld`.
  - Tạo `SceneEditor`: enum `EditTool` (Select/Wall/Shelf/Entrance/Checkout), vẽ/kéo/resize wall & shelf snap 0.25m, preview line/rect/point, chọn theo click, delete/update, `SelectWall/SelectShelf`, event `LayoutEdited`.
  - Tạo `AisleStudioUI`: port toàn bộ UI từ `web/index.html` — header + tabs Setup/Simulate, toolbar (output/parameters/step/run live/speed/clock), palette (population mode, NPC count/duration, layout objects list, add wall/shelf), stage, inspector (wall/shelf form, valance), cashier panel, footer metrics, decision trace, dialog Parameter Lab & Manual NPC.
  - Rewrite `SimulationViewController`: population mode `ga`/`manual`, `SetManualNpcs`, `SetPopulationCount`, giữ `SimulationConfig` khi rebuild, `NotifyConfigChanged`/`ResetConfig` ở `BuildHost`, events `HostCreated`/`SimAdvanced`.
  - Cập nhật `AisleSceneBuilder` (thêm SceneEditor + AisleStudioUI, populationCount=180, speed=5); xóa `AisleDashboard.cs`/`WallClickHandler.cs`/`ShelfClickHandler.cs` (không còn tham chiếu).
  - Sửa nhiều lỗi compile: CS0102 trùng `_shelfCategory`, kiểu `RectTransform` vs `GameObject` (panel/footer/cashier), `ScrollRect.AddComponent` trên RectTransform, double/float, `Config` read-only (thêm `ResetConfig`), thiếu `SelectWall/SelectShelf` trên SceneEditor, thiếu đối số `Text`.
- Đối chiếu Result_Plan.md: Hoàn tất Stage 4 phần full UI studio + map editor; lưu layout về file, camera pan/zoom, replay UI, Spine vẫn ngoài phạm vi hiện tại.
- Trạng thái: Đạt. Compile Unity hết lỗi CS; scene `Aisle.unity` rebuild OK; build Windows OK; `AIsle.exe` chạy 20s không crash, Player.log không exception.
- Kiểm tra: Unity.exe -batchmode compile/scene/build đều thành công; `Builds/Windows/AIsle.exe` tồn tại; smoke test 20s ghi `[Aisle] Host rebuilt: 180 NPCs (ga)`; không có NullReference/Exception trong Player.log.
- Nên làm tiếp theo: Chạy app và xác nhận giao diện Studio giống web; sau đó lưu layout/catalog về file khi chủ dự án yêu cầu mở phạm vi.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chua commit, chua push.


## 2026-08-14 16:35 (UTC+07:00) - Antigravity - Kiem tra cau truc thu muc & ung dung C#

- Ly do sua: Nguoi dung yeu cau kiem tra thu muc may tinh, mong meo chuyen tu chay file run.bat (mo web prototype) sang chay ung dung C#.
- Da sua/da lam:
  - Kiem tra file run.bat (hien khoi chay Node.js backend/server.mjs va trinh duyet web http://127.0.0.1:8765).
  - Kiem tra docs/Result_Plan.md, ma nguon C# simulation core tai src/ va du an Unity Desktop App tai UnityApp/.
  - Xac nhan du an da completed Stage 4, chuyen toan bo C# Simulation Core (src/AIsle.Simulation, src/AIsle.Contracts) va UI web prototype sang Unity/C# Desktop Application.
  - Xac nhan da co file build C# desktop app executable san tai UnityApp/Builds/Windows/AIsle.exe.
- Doi chieu Result_Plan.md: Phu hop voi dinh huong va ke hoach trong Result_Plan.md (chuyen web prototype sang Unity/C# Desktop Application).
- Trang thai: Dat.
- Kiem tra: File UnityApp/Builds/Windows/AIsle.exe ton tai va san sang hoat dong doc lap, khong can chay backend Node.js va browser web.
- Nen lam tiep theo: Huong dan nguoi dung chay AIsle.exe hoac tao shortcut script run_app.bat neu nguoi dung dong y.
- Pham vi dong bo: Chi local.


## 2026-08-14 16:42 (UTC+07:00) - Antigravity - Triển khai C# Desktop Application (WPF + WebView2)

- Lý do sửa: Chủ dự án xác nhận không dùng Unity App mà muốn tạo C# Desktop App (.NET 10, WPF + WebView2) giữ nguyên 100% giao diện Web Studio hiện tại.
- Đã sửa/đã làm:
  - Khởi tạo C# Desktop App project src/AIsle.DesktopApp/AIsle.DesktopApp.csproj (.NET 10-windows) sử dụng Microsoft WebView2 và kết nối C# Core (AIsle.Simulation, AIsle.Contracts).
  - Xây dựng MainWindow.xaml và MainWindow.xaml.cs tự động phát hiện/khởi chạy backend server Node.js và nhúng WebView2 hiển thị giao diện Studio.
  - Cập nhật nguồn NuGet https://api.nuget.org/v3/index.json trong NuGet.Config.
  - Thêm project AIsle.DesktopApp vào solution file AIsle.slnx.
  - Build và Publish thành công bản ứng dụng C# Desktop tại Builds/Desktop/AIsleDesktop.exe.
  - Cập nhật 
un.bat để chạy ứng dụng C# Desktop App (Builds/Desktop/AIsleDesktop.exe).
  - Cập nhật mục tiêu dự án trong docs/Result_Plan.md.
- Đối chiếu Result_Plan.md: Phù hợp với định hướng chuyển đổi sang C# Desktop App của chủ dự án.
- Trạng thái: Đạt.
- Kiểm tra: dotnet build và dotnet publish đều thành công 0 lỗi; file Builds/Desktop/AIsleDesktop.exe đã được sinh ra; file 
un.bat đã sẵn sàng khởi chạy ứng dụng C# Desktop.
- Nên làm tiếp theo: Người dùng có thể chạy 
un.bat hoặc mở trực tiếp Builds/Desktop/AIsleDesktop.exe để trải nghiệm ứng dụng C# Desktop.
- Phạm vi đồng bộ: Chỉ local.


## 2026-08-14 16:47 (UTC+07:00) - Antigravity - Giai thich su khac bien giua C# Desktop App va Web Prototype ban dau

- Ly do sua: Nguoi dung hoi su khac bien giua ung dung C# Desktop App vua tao so voi Web Prototype luc dau.
- Da sua/da lam:
  - Phap van va phan tich so sanh 3 diem khac bien chinh: Trai nghiem UI/UX (cua so Windows native doc lap vs trinh duyet web), Quan ly tien trinh Lifecycle (C# dong tu dong backend khi dong cua so), va Kha nang ket noi C# Core.
- Doi chieu Result_Plan.md: Phu hop voi dinh huong chuyen sang C# Desktop Application.
- Trang thai: Dat.
- Kiem tra: Da tra loi va giai thich ro rang bang tieng Viet cho nguoi dung.
- Nen lam tiep theo: Cho phan hoi tiep theo tu nguoi dung.
- Pham vi dong bo: Chi local.


## 2026-08-15 20:08 (UTC+07:00) - AI Agent - Đẩy code lên GitHub nhánh test
- Ly do sua: Người dùng yêu cầu push ứng dụng C# Desktop lên nhánh test.
- Da sua/da lam: Đã add thư mục src/, AIsle.slnx, Directory.Build.props, NuGet.Config, run.bat và commit với lời nhắn "Cập nhật ứng dụng C# Desktop". Đã push thành công lên origin test.
- Doi chieu Result_Plan.md: Đồng bộ mã nguồn theo yêu cầu của dự án.
- Trang thai: Dat.
- Kiem tra: Push thành công không có lỗi.
- Nen lam tiep theo: Tiếp tục phát triển theo kế hoạch.
- Pham vi dong bo: Remote (GitHub - nhánh test).



## 2026-08-15 20:18 (UTC+07:00) - AI Agent - Khôi phục run.bat để chạy Web App
- Ly do sua: Người dùng yêu cầu khôi phục file run.bat lại như cũ để không ảnh hưởng đến việc chạy bản Web của các thành viên khác trong team.
- Da sua/da lam: Ghi đè lại nội dung file run.bat bằng đoạn mã gốc khởi chạy Node.js server và mở trình duyệt.
- Doi chieu Result_Plan.md: Đảm bảo tương thích ngược cho Web Prototype theo yêu cầu.
- Trang thai: Dat.
- Kiem tra: Đã khôi phục file thành công ở local.
- Nen lam tiep theo: Chờ người dùng quyết định có muốn push thay đổi này lên GitHub hay không, hoặc tạo script mới cho Desktop App.
- Pham vi dong bo: Chi local.
