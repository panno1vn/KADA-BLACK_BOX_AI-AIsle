# Chạy AIsle & tạo dữ liệu demo

Hướng dẫn nhanh sau khi clone repo về máy mới — để có app chạy được và dashboard Thống Kê có sẵn dữ liệu, không phải nhìn màn hình trống.

## 1. Yêu cầu

- **Node.js 22+** (khuyến nghị 24+). Kiểm tra: `node --version`
- Không cần cài npm package nào — web prototype dùng vanilla JS, không có `package.json`.

## 2. Chạy app

Nhấp đúp `run.bat` ở gốc repo, hoặc từ terminal:

```powershell
run.bat
```

Launcher tự khởi động backend ở `http://127.0.0.1:8765` và mở trình duyệt. Nếu backend đã chạy sẵn, launcher chỉ mở lại tab web.

## 3. Tạo dữ liệu demo

`runtime/` (layout, catalog, lịch sử mô phỏng) **không đưa lên Git** — mỗi máy tự sinh dữ liệu riêng. Nên ngay sau khi clone, tab **"📊 Thống Kê"** sẽ trống vì chưa có lượt chạy nào.

Chạy lệnh sau (không cần app đang chạy sẵn):

```powershell
node scripts/seed-demo-data.mjs
```

Script này dùng **chính simulation engine thật** (không phải số bịa) để tự chạy khoảng 48 lượt mô phỏng, trải đều từ ~3 tháng trước tới hôm nay, rồi lưu vào `runtime/history/`. Mất khoảng 30–60 giây. Xong sẽ thấy dòng:

```text
Seeded 48 demo runs (YYYY-MM-DD → YYYY-MM-DD) into runtime/history/.
Start the app (run.bat) and open the "📊 Thống Kê" tab to see it.
```

Muốn làm mới lại data (ví dụ để mốc "hôm nay" luôn đúng ngày hiện tại) thì chạy lại **y hệt lệnh trên** bất cứ lúc nào — script chỉ xoá và tạo lại các file `demo-*.json` do chính nó sinh ra trước đó, **không đụng tới lượt chạy thật** nào tự bấm "Run live" qua giao diện.

## 4. Xem kết quả

Mở `http://127.0.0.1:8765` → tab **"📊 Thống Kê"**:

- 5 ô KPI: doanh thu, khách vào/ra, lượt mua, tỉ lệ chuyển đổi, chỉ số cảm xúc khách hàng
- 2 biểu đồ tròn: tỉ lệ chuyển đổi, cơ cấu mua hàng
- 1 biểu đồ cột theo **Ngày / Tháng / Quý / Năm** — bấm vào 1 cột để xem chi tiết kỳ đó, có nút **📅 Chọn ngày** để lật lịch xem ngày cũ hơn
- Nút **"Xem dạng bảng"** để đọc số liệu thô

## 5. Lưu ý

- File `.env` ở gốc repo **không liên quan gì tới AIsle** (code không đọc biến nào trong đó) — không cần copy, không commit lên Git.
- Mỗi lần bấm **"Run live"** trong tab Mô Phỏng và để chạy xong, kết quả tự lưu thành **1 "ngày" mới**, luôn nối tiếp ngay sau ngày gần nhất đang có trong lịch sử — không tính theo giờ máy tính thực tế.
- Nếu `run.bat` báo lỗi cổng đã dùng: tắt tiến trình `node` cũ đang chiếm port 8765 rồi chạy lại, hoặc đổi cổng bằng biến môi trường `AISLE_PORT`.
