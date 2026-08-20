# Task 11 cleanup audit

Ngày kiểm tra: 2026-08-21. Phạm vi: asset/runtime Desktop đang được `web/app.js`, WPF compatibility views và `AIsle.DesktopApp.csproj` sử dụng.

| Phân loại | Đường dẫn | Quyết định / bằng chứng |
|---|---|---|
| KEEP | `src/AIsle.DesktopApp/UI/assets/**` | Một cây asset canonical; `LocalUiAssets` kiểm tra các file runtime bắt buộc lúc khởi động. `brand/logo.png` là source-only, không tham gia UI/runtime. |
| MOVE | `UI/assets/asset/*` | Chuyển logo, music, floor, shelf, fixture vào `brand`, `audio`, `store/*`; cập nhật toàn bộ caller trước khi xóa thư mục cũ. Chọn `san.jpg` 255×255 vuông làm tile canonical. |
| MOVE | `web/cashier_*.jpg` | Chuyển vào `UI/assets/cashier`; WebView và WPF compatibility view cùng dùng một bản. |
| DELETE | `src/AIsle.DesktopApp/Assets/*` | Bản store/cashier trùng caller hoặc trùng SHA-256; caller WPF đã chuyển sang cây canonical. Bản floor cũ 263×263 không còn là tile canonical. |
| DELETE | `web/assets/asset/*` | Bản store trùng SHA-256; `web/app.js` đã trỏ tới cây canonical. |
| DELETE | `web/assets/npc/*`, `web/npc-renderer.mjs` | Bốn ảnh placeholder có cùng SHA-256 và renderer tham chiếu `npc_0..3`; runtime/package dùng renderer canonical cùng `npc_1..4`. |
| GENERATED | `.build/**` | Đã được `.gitignore` loại trừ; chỉ là output build/test/release và được tạo lại để xác minh, không phải source asset. |
| PROTECTED | `docs/**`, `backend/**`, `mobile/**`, `UnityApp/**`, contracts/history/project data | Không xóa hoặc di chuyển; ngoài phạm vi cleanup asset Task 11 và có thể là dữ liệu/công việc của thành viên khác. |

`app.ico` được sinh một lần từ `brand/logo.png` với các frame 256, 48, 32 và 16 px. Chỉ `app.ico` được đóng gói/dùng cho EXE, cửa sổ và taskbar; `logo.png` không can thiệp hệ thống và runtime không có dependency chuyển đổi ảnh.
