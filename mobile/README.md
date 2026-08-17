# Mobile Application (Expo + Metro)

Ứng dụng di động (Expo, React Native, TypeScript) hiển thị đúng 5 thông số của dashboard "📊 Thống Kê" trên web (`GET /api/analytics`) — doanh thu, khách vào/ra, lượt mua, tỉ lệ chuyển đổi, chỉ số cảm xúc khách hàng — mỗi thông số là 1 tab dưới bottom tab bar.

## Cấu trúc thư mục

```text
mobile/
├── App.tsx                      # đăng ký 5 tab, mỗi tab render MetricScreen với 1 metric config
└── src/
    ├── config.ts                # API_BASE_URL, danh sách kỳ (Ngày/Tháng/Quý/Năm)
    ├── api/analytics.ts         # gọi GET /api/analytics
    ├── state/AnalyticsContext.tsx  # fetch 1 lần, chia sẻ cho cả 5 tab (không fetch lại mỗi lần đổi tab)
    ├── utils/period.ts          # format nhãn kỳ, quy đổi chỉ số cảm xúc về thang 0-100
    ├── utils/format.ts          # định dạng tiền/số/%
    ├── components/PeriodSegment.tsx   # nút chọn Ngày/Tháng/Quý/Năm
    ├── components/MetricBarChart.tsx  # biểu đồ cột (đơn hoặc ghép đôi, có thể bấm vào cột)
    └── screens/MetricScreen.tsx  # màn hình dùng chung cho cả 5 tab
```

## Hướng dẫn cài đặt và chạy ứng dụng

1. Cài Expo Go **phiên bản 54.x** trên điện thoại (App Store / Play Store) — khớp `expo: ~54.0.35` trong `package.json`.

2. Cài dependency:
   ```powershell
   cd mobile
   npm install
   ```

3. **Bắt buộc**: khởi động backend cho phép thiết bị khác trong cùng mạng Wi-Fi kết nối tới — mặc định backend chỉ lắng nghe `127.0.0.1` (loopback), điện thoại **sẽ không kết nối được** nếu không đổi:
   ```powershell
   $env:AISLE_HOST = "0.0.0.0"
   node backend/server.mjs
   ```
   (Nếu Windows Firewall hỏi "Cho phép Node.js truy cập mạng?", chọn Cho phép — nếu chặn, điện thoại sẽ không tải được dữ liệu dù đúng IP.)

4. Kiểm tra `src/config.ts` — `API_BASE_URL` phải là địa chỉ IP mạng LAN của máy tính đang chạy backend (không phải `127.0.0.1`/`localhost`), ví dụ `http://192.168.1.27:8765`. Lấy IP bằng:
   ```powershell
   ipconfig
   ```
   (tìm dòng "IPv4 Address" của adapter Wi-Fi đang dùng). Điện thoại và máy tính phải cùng một mạng Wi-Fi.

5. Khởi chạy Metro Bundler:
   ```powershell
   npm start
   ```
   Quét mã QR bằng app Expo Go trên điện thoại.

## Dữ liệu hiển thị

Mỗi tab đọc `totals` (tổng toàn thời gian, hiện ở ô đầu màn hình) và `series.<ngày|tháng|quý|năm>` (biểu đồ cột) từ cùng 1 response `/api/analytics` — không có state giả, không có endpoint mock riêng cho mobile. Cùng dữ liệu, cùng công thức với dashboard web, chỉ khác giao diện.

Bấm vào 1 cột trên biểu đồ để xem giá trị chính xác của kỳ đó. Chỉ hiển thị tối đa 30 điểm gần nhất cho "Ngày", 24 cho "Tháng"... (xem `MAX_POINTS` trong `config.ts`) — biểu đồ tự cuộn ngang nếu cần xem thêm.

## Lưu ý

- Backend còn giữ endpoint mock cũ `GET /api/statistics-by/:type/:year` (không bị xoá) — app mobile hiện **không dùng** endpoint này nữa, toàn bộ đã chuyển sang `/api/analytics` (dữ liệu thật).
- Đã xoá `dist-check/` (build output cũ bị lỡ commit) khỏi Git — build export tạo ra `dist/`, đã nằm trong `.gitignore`, không commit.
