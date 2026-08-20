# Hướng Dẫn Tích Hợp Frontend & Backend AIsle (API & UI Integration Guide)

> **Mục đích tài liệu:** Tài liệu này liệt kê chi tiết toàn bộ các tính năng của Backend, cấu trúc API, và bản đồ liên kết tương ứng với các thành phần giao diện (nút bấm, form, canvas, dialog) ở Frontend. Khi bạn thay đổi hoặc thiết kế lại giao diện Frontend (HTML, CSS, JS Framework mới...), bạn chỉ cần tuân thủ các quy chuẩn này để đảm bảo hệ thống hoạt động trơn tru 100% mà không bị lỗi mất kết nối backend.

---

## 1. Kiến Trúc Tổng Quan

Hệ thống AIsle Web chạy theo mô hình Client-Server cục bộ:
* **Backend Server (`backend/server.mjs`):** Khởi chạy bằng Node.js tại `http://127.0.0.1:8765`, đóng vai trò vừa là Web Server phục vụ file tĩnh (`web/`), vừa cung cấp REST API JSON và tương tác dữ liệu file với thư mục `runtime/`.
* **Frontend Client (`web/`):** Tải dữ liệu từ Backend qua `fetch()`, hiển thị và thao tác bản đồ (Layout Canvas), cấu hình tham số, chạy mô phỏng bằng JS Engine (`web/live-engine.js`), và gửi kết quả/cấu hình ngược về Backend.

```
┌────────────────────────────────────────────────────────┐
│                   FRONTEND (Trình duyệt)                │
│  - web/index.html (Cấu trúc UI, Buttons, Dialogs)      │
│  - web/app.js     (Event Handlers, Canvas Draw, API)    │
│  - web/live-engine.js (Simulation Engine & Utility AI) │
└───────────────────────────┬────────────────────────────┘
                            │ REST API (JSON / fetch)
                            ▼
┌────────────────────────────────────────────────────────┐
│                   BACKEND (Node.js)                    │
│  - backend/server.mjs (HTTP Server port 8765)          │
│  - backend/routes/api-router.mjs (API Router & Rules)  │
│  - backend/storage/project-store.mjs (JSON File I/O)   │
└───────────────────────────┬────────────────────────────┘
                            │ File System I/O
                            ▼
┌────────────────────────────────────────────────────────┐
│                   RUNTIME STORAGE                      │
│  - runtime/layout.json     (Tọa độ tường, kệ, cửa)     │
│  - runtime/catalog.json    (Danh mục hàng hóa & giá)    │
│  - runtime/live_result.json(Kết quả lượt chạy mới nhất)│
│  - runtime/history/*.json  (Lịch sử các phiên mô phỏng)│
└────────────────────────────────────────────────────────┘
```

---

## 2. Chi Tiết Toàn Bộ API Endpoints Của Backend

Hàm gọi API chuẩn từ Frontend (được định nghĩa trong `web/app.js`):
```javascript
async function api(path, options = {}) {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.error || response.statusText);
  return data;
}
```

---

### 2.1. `GET /health`
* **Mô tả:** Kiểm tra trạng thái máy chủ Backend.
* **Request:** Không có Body.
* **Response (200 OK):**
  ```json
  {
    "ok": true,
    "engine": "javascript-live"
  }
  ```

---

### 2.2. `GET /api/project`
* **Mô tả:** Lấy thông tin bản đồ siêu thị (`layout`) và danh mục sản phẩm (`catalog`) hiện tại để hiển thị lên màn hình.
* **Thời điểm gọi:** Khi mở trang web (`init()`).
* **Response (200 OK):**
  ```json
  {
    "layout": {
      "width": 32,
      "height": 24,
      "walls": [
        { "id": "w1", "x1": 0.25, "y1": 0.25, "x2": 0.25, "y2": 23.75, "length": 23.5 }
      ],
      "shelves": [
        { "id": "s2", "label": "Do an nhanh", "category": "instant-food", "x": 4.75, "y": 17.95, "w": 4.75, "h": 2.5, "valence": 0.25 }
      ],
      "entrance": { "x": 14, "y": 22.5 },
      "checkout": { "x": 16.25, "y": 22.5 }
    },
    "catalog": [
      { "id": "p001", "name": "Banh mi tuoi", "category": "bakery", "shelf": "s2", "price": 12000 }
    ]
  }
  ```

---

### 2.3. `POST /api/project`
* **Mô tả:** Lưu thiết kế layout (tường, kệ, lối vào, quầy tính tiền) và danh mục sản phẩm xuống file `runtime/layout.json` và `runtime/catalog.json`.
* **Thời điểm gọi:** Khi người dùng thêm/xóa/sửa kệ, tường, di chuyển cửa hoặc chỉnh thông số kệ.
* **Request Body:**
  ```json
  {
    "layout": { ... },
    "catalog": [ ... ]
  }
  ```
* **Validation Backend:** Tự động kiểm tra `validateLayout(project.layout)`. Nếu có lỗi (lối vào trùng quầy thanh toán, kệ ngoài biên...) sẽ trả về lỗi `400`.
* **Response (200 OK):**
  ```json
  {
    "ok": true,
    "warnings": [],
    "unreachableShelfIds": []
  }
  ```

---

### 2.4. `POST /api/live-result`
* **Mô tả:** Lưu kết quả mô phỏng đang chạy vào file tạm `runtime/live_result.json`.
* **Request Body:** Toàn bộ đối tượng `SimResult` JSON.
* **Response (200 OK):**
  ```json
  {
    "ok": true
  }
  ```

---

### 2.5. `GET /api/history`
* **Mô tả:** Lấy danh sách tóm tắt toàn bộ các lượt mô phỏng đã lưu trong `runtime/history/`.
* **Response (200 OK):**
  ```json
  {
    "runs": [
      {
        "id": "run-1786700000000",
        "schemaVersion": "1.0",
        "createdAt": "2026-08-18T00:00:00.000Z",
        "name": "Layout A — live test",
        "seed": 42,
        "durationMinutes": 30,
        "summary": {
          "totalNpcs": 180,
          "converted": 120,
          "revenue": 1560000
        }
      }
    ]
  }
  ```

---

### 2.6. `POST /api/history`
* **Mô tả:** Lưu một kết quả mô phỏng hoàn chỉnh thành một bản ghi lịch sử mới trong `runtime/history/<id>.json`.
* **Request Body:** Toàn bộ payload `SimResult` (có `id`, `name`, `input`, `summary`, `agents`...).
* **Response (201 Created):** Trả về summary của bản ghi vừa lưu.

---

### 2.7. `GET /api/history/:id`
* **Mô tả:** Lấy chi tiết toàn bộ dữ liệu của một bản ghi lịch sử theo `id`.
* **Response (200 OK):** Chi tiết file JSON của lượt chạy đó.

---

### 2.8. `GET /api/statistics-by/:type/:year`
* **Mô tả:** Lấy dữ liệu thống kê báo cáo theo kỳ (`type`: `thang`, `quy`, hoặc `nam`) của một năm cụ thể.
* **Ví dụ:** `/api/statistics-by/thang/2026`
* **Response (200 OK):**
  ```json
  {
    "percent": [
      { "key": 1, "value": "8%" },
      { "key": 2, "value": "11%" }
    ],
    "numberOfPurchases": 4725
  }
  ```

---

## 3. Bản Đồ Liên Kết Giữa Giao Diện Frontend và Backend

Bảng dưới đây mô tả chính xác từng phần tử HTML (ID/Class), sự kiện kích hoạt và hàm xử lý tương ứng trong Frontend:

| Tên chức năng | Phần tử HTML (ID / Selector) | Sự kiện | Hàm JS xử lý | API Backend liên quan |
| :--- | :--- | :--- | :--- | :--- |
| **Khởi động trang** | `window` / `body` | `DOMContentLoaded` | `init()` | `GET /api/project` |
| **Chuyển Tab (Setup / Simulate)** | `.tab-btn[data-tab="setup"]`<br>`.tab-btn[data-tab="simulate"]` | `click` | `switchTab(tab)` | Không (UI state) |
| **Đổi Tên Phiên Chạy** | `#run-name` | `input` / `change` | `currentSimResult()` | Lưu vào tên khi export / history |
| **Xuất File Kết Quả** | `#export-btn` | `click` | `exportSimulation()` | Tải file `.sim-result.json` |
| **Chọn Công Cụ Vẽ** | `.tools button[data-tool]` | `click` | Đổi biến `tool` (select, wall, shelf, entrance, checkout) | Không (UI Canvas) |
| **Thêm Bức Tường Mới** | `#add-wall` | `click` | `layout.walls.push(...)` -> `saveProject()` | `POST /api/project` |
| **Thêm Kệ Hàng Mới** | `#add-shelf` | `click` | `layout.shelves.push(...)` -> `saveProject()` | `POST /api/project` |
| **Kéo Thả / Vẽ Trên Canvas** | `#scene` (`<canvas>`) | `pointerdown`<br>`pointermove`<br>`pointerup` | `pointerDown()`<br>`pointerMove()`<br>`pointerUp()` -> `saveProject()` | `POST /api/project` |
| **Chỉnh Sửa Thông Số Tường** | `#wall-x1`, `#wall-y1`<br>`#wall-x2`, `#wall-y2` | `input` | `updateWall()` -> `saveProject()` | `POST /api/project` |
| **Xóa Tường** | `#delete-wall` | `click` | `deleteSelected('wall')` -> `saveProject()` | `POST /api/project` |
| **Chỉnh Sửa Thông Số Kệ** | `#shelf-label`, `#shelf-category`<br>`#shelf-valence`, `#shelf-x`<br>`#shelf-y`, `#shelf-w`, `#shelf-h` | `input` | `updateShelf()` -> `saveProject()` | `POST /api/project` |
| **Xóa Kệ** | `#delete-shelf` | `click` | `deleteSelected('shelf')` -> `saveProject()` | `POST /api/project` |
| **Chọn Chế Độ NPC (GA / Manual)** | `#population-mode` | `change` | `markDirty()` | Không (Engine state) |
| **Số Lượng NPC** | `#npc-count` | `input` | Cập nhật `#npc-output` | Không (Engine state) |
| **Thời Gian Mô Phỏng** | `#duration` | `input` | Cập nhật `#duration-output` | Không (Engine state) |
| **Mở Hộp Thoại Nhập NPC Thủ Công** | `#manual-btn` | `click` | `openManual()` mở `#manual-dialog` | Không |
| **Áp Dụng NPC Thủ Công** | `#apply-manual` | `click` | `applyManual()` đọc `#manual-editor` | Không |
| **Mở Bảng Tham Số (Lab)** | `#parameter-btn` | `click` | Mở `#parameter-dialog` | Không |
| **Áp Dụng Tham Số Mô Phỏng** | `#apply-parameters` | `click` | `applyParameters()` cập nhật `parameters` | Không |
| **Reset Tham Số Mặc Định** | `#reset-parameters` | `click` | `parameters = {...DEFAULT_PARAMETERS}` | Không |
| **Chạy / Tạm Dừng Mô Phỏng** | `#play-btn` | `click` | `toggleRun()` | `POST /api/history` (khi kết thúc) |
| **Tua Từng Bước (Single Step)** | `#step-btn` | `click` | `singleStep()` | Không |
| **Reset Lượt Mô Phỏng** | `#reset-btn` | `click` | `resetSimulation()` | Không |
| **Tốc Độ Mô Phỏng (Speed)** | `#speed` | `change` | Thay đổi tốc độ vòng lặp render | Không |
| **Thanh Tua Thời Gian** | `#timeline` | `change` | `seekTo(...)` | Không |
| **Thu Ngân (Tương Tác & Cảm Xúc)** | `#cashier-avatar`<br>`#cashier-mood`<br>`#cashier-served`<br>`#cashier-revenue` | Realtime event | `updateCashier()`, `triggerCashierReaction()` | Cập nhật theo doanh thu từ Engine |

---

## 4. Các Lưu Ý Quan Trọng Khi Thiết Kế Lại Giao Diện (Frontend Refactor)

Nếu bạn muốn thay đổi toàn bộ HTML/CSS hoặc chuyển sang dùng React, Vue, Svelte, Tailwind CSS:

1. **Giữ nguyên các Endpoint API & cấu trúc JSON:**
   * Backend nhận `layout` chứa mảng `walls`, `shelves`, `entrance` ({x, y}), `checkout` ({x, y}).
   * Backend nhận `catalog` chứa mảng các item có: `id`, `name`, `category`, `shelf`, `price`.
2. **Không bỏ qua khâu Validation Layout:**
   * Backend sẽ từ chối lưu (`400 Bad Request`) nếu tọa độ kệ nằm ngoài biên hoặc cửa ra vào trùng vị trí quầy thu ngân.
3. **Nếu viết lại bằng Framework (React/Vue):**
   * Bạn có thể gom các hàm gọi API thành một service file `src/services/api.js`.
   * Tách Canvas render thành 1 Component riêng nhận props `layout`, `agents`, `playing`.
   * Nhân vật thu ngân chỉ cần lắng nghe sự kiện `onPurchase` từ engine mô phỏng để đổi state hình ảnh/mood text.

---

## 5. Quy Trình Chạy & Test Local

1. Khởi động Backend + Frontend: Chạy file `run.bat` ở thư mục gốc.
2. Kiểm tra Backend có nhận dữ liệu: Mở tab Network trong F12 Trình duyệt, kiểm tra các request `/api/project`.
3. Kiểm tra tính năng lưu: Thêm 1 kệ hàng mới, tải lại trang (F5). Nếu kệ hàng vẫn còn ở vị trí vừa thêm nghĩa là liên kết API `POST /api/project` và `GET /api/project` đã hoạt động chính xác.
