# Mobile Application (Expo + Metro)

Ứng dụng di động được xây dựng bằng React Native (Expo) dùng để hiển thị biểu đồ thống kê doanh thu và lượt mua hàng.

## Cấu trúc thư mục

```text
mobile/
├── App.tsx                # 2 tab: Doanh thu | Mua hàng
└── src/
    ├── config.ts          # API_BASE_URL, năm min, loại thống kê
    ├── api/statistics.ts  # gọi GET /statistics-by/:type/:year
    ├── components/        # YearPicker (lazy FlatList), StatPieChart
    └── screens/           # RevenueScreen, PurchasesScreen (trống)
```

## Hướng dẫn cài đặt và chạy ứng dụng

1. Di chuyển vào thư mục mobile:
   ```powershell
   cd mobile
   ```

2. Cài đặt các gói phụ thuộc:
   ```powershell
   npm install
   ```

3. Khởi chạy Metro Bundler và Expo Go:
   ```powershell
   npm start
   ```

## Cấu hình kết nối API

Thay đổi giá trị `API_BASE_URL` trong tệp `src/config.ts` thành địa chỉ IP mạng nội bộ của máy tính của bạn khi chạy thử nghiệm trên thiết bị thật (Android Emulator thường dùng địa chỉ mặc định `http://10.0.2.2:8765`).
